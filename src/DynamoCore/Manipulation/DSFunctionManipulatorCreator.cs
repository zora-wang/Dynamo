using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dynamo.Manipulation;
using Dynamo.Models;
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
                        new INodeManipulatorCreator[] { new KeyboardPointManipulatorCreator(), 
                            new MousePointManipulatorCreator() }
                    }
                })
        { }

        protected override string GetKey(DSFunction dsfunc)
        {
            return dsfunc.Definition.MangledName;
        }
    }
}
