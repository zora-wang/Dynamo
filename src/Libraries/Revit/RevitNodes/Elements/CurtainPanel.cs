using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.DesignScript.Geometry;
using Autodesk.Revit.DB;
using DSNodeServices;
using Revit.GeometryConversion;
using Revit.References;
using RevitServices.Persistence;
using RevitServices.Transactions;
using Face = Autodesk.DesignScript.Geometry.Face;
using Plane = Autodesk.DesignScript.Geometry.Plane;


namespace Revit.Elements
{
   /// <summary>
   /// A Revit CurtainGrid
   /// </summary>
   public class CurtainPanel : AbstractFamilyInstance
   {
      #region Properties

      protected CurveArrArray PanelBoundaries
      {
         get
         {
            // This creates a new wall and deletes the old one
            TransactionManager.Instance.EnsureInTransaction(Document);

            var elementAsPanel = InternalElement as Autodesk.Revit.DB.Panel;
            if (elementAsPanel == null)
               throw new Exception("InternalElement of Curtain Panel is not Panel");

            var host = elementAsPanel.Host;

            var hostingGrid = CurtainGrid.ByElement(UnknownElement.FromExisting(host));

            ElementId uGridId = ElementId.InvalidElementId;
            ElementId vGridId = ElementId.InvalidElementId;
            elementAsPanel.GetRefGridLines(ref uGridId, ref vGridId);

            CurtainCell cell = hostingGrid.InternalCurtainGrid.GetCell(uGridId, vGridId);

            TransactionManager.Instance.TransactionTaskDone();

            if (cell == null)
               throw new Exception("Could not find cell for panel");
            return cell.CurveLoops;
         }
      }

      public PolyCurve[] Boundaries
      {
         get
         {
            var enumCurveLoops = PanelBoundaries.GetEnumerator();
            var bounds = new List<PolyCurve>();

            for (; enumCurveLoops.MoveNext();)
            {
               var crvs = new CurveArray();
               var crvArr = (CurveArray) enumCurveLoops.Current;
               var enumCurves = crvArr.GetEnumerator();
               for (; enumCurves.MoveNext();)
               {
                  var crv = (Autodesk.Revit.DB.Curve) enumCurves.Current;
                  crvs.Append(crv);
               }
               bounds.Add(Revit.GeometryConversion.RevitToProtoCurve.ToProtoTypes(crvs));
            }
            return bounds.ToArray();
         }
      }

      public bool HasPlane
      {
         get
         {
            var enumCurveLoops = PanelBoundaries.GetEnumerator();
            Autodesk.Revit.DB.Plane plane = null;
            for (; enumCurveLoops.MoveNext();)
            {
               var cLoop = new CurveLoop();
               var crvArr = (CurveArray) enumCurveLoops.Current;
               var enumCurves = crvArr.GetEnumerator();
               for (; enumCurves.MoveNext();)
               {
                  var crv = (Autodesk.Revit.DB.Curve) enumCurves.Current;
                  cLoop.Append(crv);
               }
               if (!cLoop.HasPlane())
                  return false;
               var thisPlane = cLoop.GetPlane();
               if (plane == null)
                  plane = thisPlane;
               else if (Math.Abs(plane.Normal.DotProduct(thisPlane.Normal)) < 1.0 - 1.0e-9)
                  return false;
               else
               {
                  if (Math.Abs((plane.Origin - thisPlane.Origin).DotProduct(plane.Normal)) > 1.0e-9)
                     return false;
               }
            }
            return true;
         }
      }

      public Plane PanelPlane
      {
         get
         {
            var enumCurveLoops = PanelBoundaries.GetEnumerator();
            Autodesk.Revit.DB.Plane plane = null;
            for (; enumCurveLoops.MoveNext();)
            {
               var cLoop = new CurveLoop();
               var crvArr = (CurveArray) enumCurveLoops.Current;
               var enumCurves = crvArr.GetEnumerator();
               for (; enumCurves.MoveNext();)
               {
                  var crv = (Autodesk.Revit.DB.Curve) enumCurves.Current;
                  cLoop.Append(crv);
               }
               if (!cLoop.HasPlane())
                  throw new Exception(" Curtain Panel is not planar");
               var thisPlane = cLoop.GetPlane();
               if (plane == null)
                  plane = thisPlane;
               else if (Math.Abs(plane.Normal.DotProduct(thisPlane.Normal)) < 1.0 - 1.0e-9)
                  throw new Exception(" Curtain Panel is not planar");
               else
               {
                  if (Math.Abs((plane.Origin - thisPlane.Origin).DotProduct(plane.Normal)) > 1.0e-9)
                     throw new Exception(" Curtain Panel is not planar");
               }
            }
            if (plane == null)
               throw new Exception(" Curtain Panel is not planar");
            Plane result = Plane.ByOriginNormal(plane.Origin.ToPoint(), plane.Normal.ToVector());
            return result;
         }
      }

      public double Length
      {
         get
         {
            double lengthVal = 0.0;
            var enumCurveLoops = PanelBoundaries.GetEnumerator();
            Autodesk.Revit.DB.Plane plane = null;
            for (; enumCurveLoops.MoveNext();)
            {
               var cLoop = new CurveLoop();
               var crvArr = (CurveArray) enumCurveLoops.Current;
               var enumCurves = crvArr.GetEnumerator();
               for (; enumCurves.MoveNext();)
               {
                  var crv = (Autodesk.Revit.DB.Curve) enumCurves.Current;
                  lengthVal += crv.Length;
               }
            }
            return lengthVal;
         }
      }

      public bool IsRectangular
      {
         get
         {
            var enumCurveLoops = PanelBoundaries.GetEnumerator();
            int num = 0;
            bool result = false;
            for (; enumCurveLoops.MoveNext();)
            {
               if (num > 0)
                  return false;
               num++;
               var cLoop = new CurveLoop();
               var crvArr = (CurveArray) enumCurveLoops.Current;
               var enumCurves = crvArr.GetEnumerator();
               for (; enumCurves.MoveNext();)
               {
                  var crv = (Autodesk.Revit.DB.Curve) enumCurves.Current;
                  cLoop.Append(crv);
               }
               if (!cLoop.HasPlane())
                  return false;
               result = cLoop.IsRectangular(cLoop.GetPlane());
            }
            return result;
         }
      }

      public double GetRectangularWidth
      {
         get
         {
            var enumCurveLoops = PanelBoundaries.GetEnumerator();
            int num = 0;
            double result = 0.0;
            for (; enumCurveLoops.MoveNext();)
            {
               if (num > 0)
                  throw new Exception(" Curtain Panel is not rectangular");
               num++;
               var cLoop = new CurveLoop();
               var crvArr = (CurveArray) enumCurveLoops.Current;
               var enumCurves = crvArr.GetEnumerator();
               for (; enumCurves.MoveNext();)
               {
                  var crv = (Autodesk.Revit.DB.Curve) enumCurves.Current;
                  cLoop.Append(crv);
               }
               if (!cLoop.HasPlane())
                  throw new Exception(" Curtain Panel is not rectangular");
               if (!cLoop.IsRectangular(cLoop.GetPlane()))
                  throw new Exception(" Curtain Panel is not rectangular");
               result = cLoop.GetRectangularWidth(cLoop.GetPlane());
            }
            return result;
         }
      }

      public double GetRectangularHeight
      {
         get
         {
            var enumCurveLoops = PanelBoundaries.GetEnumerator();
            int num = 0;
            double result = 0.0;
            for (; enumCurveLoops.MoveNext();)
            {
               if (num > 0)
                  throw new Exception(" Curtain Panel is not rectangular");
               num++;
               var cLoop = new CurveLoop();
               var crvArr = (CurveArray) enumCurveLoops.Current;
               var enumCurves = crvArr.GetEnumerator();
               for (; enumCurves.MoveNext();)
               {
                  var crv = (Autodesk.Revit.DB.Curve) enumCurves.Current;
                  cLoop.Append(crv);
               }
               if (!cLoop.HasPlane())
                  throw new Exception(" Curtain Panel is not rectangular");
               if (!cLoop.IsRectangular(cLoop.GetPlane()))
                  throw new Exception(" Curtain Panel is not rectangular");
               result = cLoop.GetRectangularHeight(cLoop.GetPlane());
            }
            return result;
         }
      }

      #endregion

      #region Private constructors

      /// <summary>
      /// Create from an existing Revit Element
      /// </summary>
      /// <param name="panelElement"></param>
      protected CurtainPanel(Autodesk.Revit.DB.Panel panelElement)
      {
         InternalSetFamilyInstance(panelElement);
      }

      #endregion

      #region Static constructors

      /// <summary>
      ///get curtain panel from element  
      /// </summary>
      /// <param name="panelElement"></param>

      public static CurtainPanel ByElement(CurtainPanel panelElement)
      {
         var elementAsPanel = panelElement.InternalElement as Autodesk.Revit.DB.Panel;
         if (elementAsPanel == null)
            throw new Exception("curtain Panel should be Family Instance");
         return new CurtainPanel(elementAsPanel);
      }

      /// <summary>
      /// Construct this type from an existing Revit element.
      /// </summary>
      /// <param name="panel"></param>
      /// <param name="isRevitOwned"></param>
      /// <returns></returns>
      internal static CurtainPanel FromExisting(Autodesk.Revit.DB.Panel panel, bool isRevitOwned)
      {
         if (panel == null)
         {
            throw new ArgumentNullException("panel");
         }

         return new CurtainPanel(panel)
         {
            IsRevitOwned = true //making panels in Dynamo is not implemented
         };
      }

      #endregion

      #region public methods

      public FamilyInstance AsFamilyInstance()
      {
         return FamilyInstance.FromExisting(InternalElement as Autodesk.Revit.DB.FamilyInstance, true);
      }

      public override string ToString()
      {
         return "Curtain Panel";
      }

      #endregion

   }
}