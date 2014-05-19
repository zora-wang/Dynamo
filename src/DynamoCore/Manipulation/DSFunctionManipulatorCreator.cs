using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dynamo.Nodes;

namespace Dynamo.Manipulation
{
    public class DSFunctionManipulatorCreator : INodeManipulatorCreator
    {
        private readonly Dictionary<string, INodeManipulatorCreator> ManipulatorCreators = new Dictionary<string, INodeManipulatorCreator>()
            {
                {"Autodesk.DesignScript.Geometry.Point.ByCoordinates@double,double,double", new KeyboardPointManipulatorCreator()}
            };

        public IManipulator Create(Models.NodeModel node, DynamoContext context)
        {
            var dsfunc = node as DSFunction;
            if (dsfunc == null) return null;

            var name = dsfunc.Definition.MangledName;

            return ManipulatorCreators.ContainsKey(name) ? 
                ManipulatorCreators[name].Create(node, context) : null;
        }
    }
}
