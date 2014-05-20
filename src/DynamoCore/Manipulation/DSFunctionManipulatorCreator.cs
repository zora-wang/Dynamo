using System;
using System.Collections.Generic;
using System.Text;
using Dynamo.Nodes;

namespace Dynamo.Manipulation
{
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
}
