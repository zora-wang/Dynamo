using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using Autodesk.DesignScript.Geometry;
using Autodesk.Revit.DB;
using DSNodeServices;
using Revit.GeometryConversion;

using RevitServices.Materials;
using RevitServices.Persistence;
using RevitServices.Transactions;

namespace Revit.Elements
{
    [RegisterForTrace]
    public class DirectShape : Element
    {
        [Browsable(false)]
        public override Autodesk.Revit.DB.Element InternalElement
        {
            get { return InternalDirectShape; }
        }

        internal Autodesk.Revit.DB.DirectShape InternalDirectShape { get; private set; }

        internal string _directShapeGUID;
        internal static string _directShapeAppGUID = Guid.NewGuid().ToString();

        internal DirectShape(ImportInstance importInstance, Category category)
        {
            TransactionManager.Instance.EnsureInTransaction(Document);

            var goptions = new Options
            {
                IncludeNonVisibleObjects = true,
                ComputeReferences = true
            };

            GeometryElement revitGeometry = importInstance.InternalImportInstance.get_Geometry(goptions);

            _directShapeGUID = Guid.NewGuid().ToString();

            Autodesk.Revit.DB.DirectShape ds = Autodesk.Revit.DB.DirectShape.CreateElement(
                Document, category.InternalCategory.Id, _directShapeAppGUID, _directShapeGUID);

            ds.SetShape(CollectConcreteGeometry(revitGeometry).ToList());

            InternalSetDirectShape(ds);

            TransactionManager.Instance.TransactionTaskDone();

            ElementBinder.SetElementForTrace(ds);
        }

        public static DirectShape ByImportInstance(ImportInstance importInstance, Category category)
        {
            if (importInstance == null)
                throw new ArgumentNullException("geometry");

            return new DirectShape(importInstance, category);
        }

        private void InternalSetDirectShape(Autodesk.Revit.DB.DirectShape ds)
        {
            this.InternalUniqueId = ds.UniqueId;
            this.InternalElementId = ds.Id;
            this.InternalDirectShape = ds;
        }
    }
}
