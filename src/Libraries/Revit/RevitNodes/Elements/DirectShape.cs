using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.DesignScript.Geometry;
using Autodesk.Revit.DB;

using Revit.GeometryConversion;

using RevitServices.Persistence;
using RevitServices.Transactions;

namespace Revit.Elements
{
    public class DirectShape : Element
    {
        private string dynamoGuid = "5039f62b-cd47-4524-929a-cda035c12a92";

        /// <summary>
        /// Internal variable containing the wrapped Revit object
        /// </summary>
        internal Autodesk.Revit.DB.DirectShape InternalDirectShape
        {
            get;
            private set;
        }

        /// <summary>
        /// Reference to the Element
        /// </summary>
        public override Autodesk.Revit.DB.Element InternalElement
        {
            get { return InternalDirectShape; }
        }

        private DirectShape(List<GeometryObject> geometry, Autodesk.Revit.DB.Category category)
        {
            //Phase 1 - Check to see if the object exists and should be rebound
            var oldDirectShape =
                ElementBinder.GetElementFromTrace<Autodesk.Revit.DB.DirectShape>(Document);

            //There was an element, rebind to that, and adjust its position
            if (oldDirectShape != null)
            {
                if (oldDirectShape.Category == category)
                {
                    InternalSetDirectShape(oldDirectShape);
                    InternalSetGeometry(geometry);
                    return;
                }
            }

            //Phase 2- There was no existing element, create one
            TransactionManager.Instance.EnsureInTransaction(Document);

            // There is no API to reset the category of a direct 
            // shape, so we have to delete the old one.
            if (oldDirectShape != null)
            {
                DocumentManager.Instance.CurrentDBDocument.Delete(oldDirectShape.Id);
            }

            var ds =
                Autodesk.Revit.DB.DirectShape.CreateElement(
                    DocumentManager.Instance.CurrentDBDocument,
                    category.Id, dynamoGuid, dynamoGuid);
            ds.SetShape(geometry);

            InternalSetDirectShape(ds);

            TransactionManager.Instance.TransactionTaskDone();

            ElementBinder.SetElementForTrace(InternalElement);
        }

        public static DirectShape ByGeometryAndCategory(
            Geometry geometry, Revit.Elements.Category category)
        {
            if (geometry == null)
            {
                throw new ArgumentNullException("geometry");
            }

            if (category == null)
            {
                throw new ArgumentNullException("category");
            }

            var geobs = new List<GeometryObject>();
            ConvertToGeometryObject(geometry, ref geobs);

            return new DirectShape(geobs, category.InternalCategory);
        }

        internal void InternalSetDirectShape(Autodesk.Revit.DB.DirectShape ds)
        {
            InternalDirectShape = ds;
            InternalElementId = ds.Id;
            InternalUniqueId = ds.UniqueId;
        }

        internal void InternalSetGeometry(List<GeometryObject> geoms)
        {
            InternalDirectShape.SetShape(geoms);
        }

        private static void ConvertToGeometryObject(Geometry geometry, ref List<GeometryObject> geobs)
        {
            var geom = geometry as PolyCurve;
            if (geom != null)
            { 
                geobs.AddRange(geom.Curves().Select(c => c.ToRevitType()).Cast<GeometryObject>());
            }

            var point = geometry as Autodesk.DesignScript.Geometry.Point;
            if (point != null)
            {
                geobs.Add(DocumentManager.Instance.CurrentUIApplication.Application.Create.NewPoint(point.ToXyz()));
            }

            var curve = geometry as Autodesk.DesignScript.Geometry.Curve;
            if (curve != null)
            {
                geobs.Add(curve.ToRevitType());
            }

            var surf = geometry as Surface;
            if (surf != null)
            {
                geobs.AddRange(surf.ToRevitType());
            }

            var solid = geometry as Autodesk.DesignScript.Geometry.Solid;
            if (solid != null)
            {
                geobs.AddRange(solid.ToRevitType());
            }
        }
    }
}
