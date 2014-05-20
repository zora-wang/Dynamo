using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dynamo.Models;
using Dynamo.Nodes;

namespace Dynamo.Manipulation
{
    public abstract class LookupCreator<T> : INodeManipulatorCreator where T : NodeModel
    {
        protected Dictionary<string, IEnumerable<INodeManipulatorCreator>> ManipulatorCreators { get; private set; }

        protected LookupCreator(Dictionary<string, IEnumerable<INodeManipulatorCreator>> manipulatorCreators)
        {
            ManipulatorCreators = manipulatorCreators;
        }

        protected LookupCreator() : this(new Dictionary<string, IEnumerable<INodeManipulatorCreator>>()) { }

        public IManipulator Create(NodeModel node, DynamoContext context)
        {
            var dsfunc = node as T;
            if (dsfunc == null) return null;

            var name = GetKey(dsfunc);

            return ManipulatorCreators.ContainsKey(name)
                ? new CompositeManipulator(ManipulatorCreators[name].Select(m => m.Create(node, context)))
                : null;
        }

        protected abstract string GetKey(T dsfunc);
    }

    public class DSFunctionManipulatorCreator : LookupCreator<DSFunction>
    {
        public DSFunctionManipulatorCreator()
            : base(
                new Dictionary<string, IEnumerable<INodeManipulatorCreator>>
                {
                    {
                        "Autodesk.DesignScript.Geometry.Point.ByCoordinates@double,double,double",
                        new[] { new KeyboardPointManipulatorCreator() }
                    }
                })
        { }

        protected override string GetKey(DSFunction dsfunc)
        {
            return dsfunc.Definition.MangledName;
        }
    }

    public class CustomNodeManipulatorCreator : LookupCreator<Function>
    {
        private static CustomNodeManipulatorCreator instance;
        public static CustomNodeManipulatorCreator Instance
        {
            get { return instance ?? (instance = new CustomNodeManipulatorCreator()); }
        }

        private CustomNodeManipulatorCreator() { }

        protected override string GetKey(Function dsfunc)
        {
            return dsfunc.Definition.FunctionId.ToString();
        }
    }
}
