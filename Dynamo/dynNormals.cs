//Copyright 2012 Ian Keough

//Licensed under the Apache License, Version 2.0 (the "License");
//you may not use this file except in compliance with the License.
//You may obtain a copy of the License at

//http://www.apache.org/licenses/LICENSE-2.0

//Unless required by applicable law or agreed to in writing, software
//distributed under the License is distributed on an "AS IS" BASIS,
//WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//See the License for the specific language governing permissions and
//limitations under the License.

using Autodesk.Revit.DB;

using Dynamo.Connectors;

using Microsoft.FSharp.Collections;
using Expression = Dynamo.FScheme.Expression;

namespace Dynamo.Elements
{
    [ElementName("Evaluate Normal")]
    [ElementCategory(BuiltinElementCategories.REVIT_XYZ_UV_VECTOR)]
    [ElementDescription("Evaluate a point on a face to find the normal.")]
    [RequiresTransaction(false)]
    class dynNormalEvaluate:dynNode
    {
        public dynNormalEvaluate()
        {
            InPortData.Add(new PortData("uv", "The point (UV or RefPoint) to evaluate.", typeof(object)));
            InPortData.Add(new PortData("face", "The face to evaluate.", typeof(object)));
            
            OutPortData = new PortData("XYZ", "The normal.", typeof(string));
            base.RegisterInputsAndOutputs();
        }

        public override Expression Evaluate(FSharpList<Expression> args)
        {

            var ptA = ((Expression.Container)args[0]).Item;
            Reference faceRef = (args[1] as Expression.Container).Item as Reference;
            
            Face f = this.UIDocument.Document.GetElement(faceRef).GetGeometryObjectFromReference(faceRef) as Face;
            XYZ norm = null;
            
            if (f != null)
            {

                if (ptA is UV)
                {
                    //each item in the list will be a UV
                    UV uv = (UV)ptA;
                    norm = f.ComputeNormal(uv);
                }
                else if (ptA is PointOnFace)
                {
                    //each item in the list will be a RefPointOnFace 
                    PointOnFace pof = (PointOnFace)ptA;
                    UV uv = (UV)pof.UV;
                    norm = f.ComputeNormal(uv);
                }

            }

            return Expression.NewContainer(norm);
        }
    }

    [ElementName("Evaluate UV")]
    [ElementCategory(BuiltinElementCategories.REVIT_XYZ_UV_VECTOR)]
    [ElementDescription("Evaluate a parameter(UV or RefPoint) on a face to find the XYZ location.")]
    [RequiresTransaction(false)]
    class dynXYZEvaluate : dynNode
    {
        public dynXYZEvaluate()
        {
            InPortData.Add(new PortData("uv", "The point to evaluate.", typeof(object)));
            InPortData.Add(new PortData("face", "The face to evaluate.", typeof(object)));
            
            OutPortData = new PortData("XYZ", "The location.", typeof(string));
            base.RegisterInputsAndOutputs();
        }

        public override Expression Evaluate(FSharpList<Expression> args)
        {
            var ptA = ((Expression.Container)args[0]).Item;
            Reference faceRef = (args[1] as Expression.Container).Item as Reference;

            Face f = this.UIDocument.Document.GetElement(faceRef).GetGeometryObjectFromReference(faceRef) as Face;
            XYZ face_point = null;

            if (f != null)
            {
                if (ptA is UV)
                {
                    //each item in the list will be a UV
                    UV uv = (UV)ptA;
                    face_point = f.Evaluate(uv);
                }
                else if (ptA is PointOnFace)
                {
                    //each item in the list will be a RefPointOnFace 
                    PointOnFace pof = (PointOnFace)ptA;
                    UV uv = (UV)pof.UV;
                    face_point = f.Evaluate(uv);
                }
            }
            return Expression.NewContainer(face_point);
        }
    }

}
