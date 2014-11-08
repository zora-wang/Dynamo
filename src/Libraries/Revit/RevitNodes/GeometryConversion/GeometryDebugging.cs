using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Autodesk.DesignScript.Geometry;
using Autodesk.Revit.DB;

using Element = Revit.Elements.Element;
using Face = Autodesk.Revit.DB.Face;

namespace Revit.GeometryConversion
{
#if DEBUG

    public static class SolidDebugging
    {
        public static IEnumerable<Autodesk.Revit.DB.Solid> GetRevitSolids(Element ele)
        {
            return ele.InternalGeometry().OfType<Autodesk.Revit.DB.Solid>().ToArray();
        }

        public static IEnumerable<Autodesk.DesignScript.Geometry.Surface> GetTrimmedSurfacesFromSolid(Autodesk.Revit.DB.Solid geom)
        {
            return geom.Faces.Cast<Autodesk.Revit.DB.Face>().SelectMany(x => x.ToProtoType(false));
        }

        public static IEnumerable<Autodesk.DesignScript.Geometry.Surface> GetTrimmedSurfacesFromFace(Autodesk.Revit.DB.Face geom)
        {
            return geom.ToProtoType(false);
        }

        public static IEnumerable<Autodesk.Revit.DB.Face> GetRevitFaces(Autodesk.Revit.DB.Solid geom)
        {
            return geom.Faces.Cast<Autodesk.Revit.DB.Face>();
        }

        public static IEnumerable<IEnumerable<Autodesk.Revit.DB.Edge>> GetEdgeLoopsFromRevitFace(Autodesk.Revit.DB.Face face)
        {
            return face.EdgeLoops.Cast<EdgeArray>()
                .Select(x => x.Cast<Autodesk.Revit.DB.Edge>());
        }

        public static Autodesk.DesignScript.Geometry.Surface GetUntrimmedSurfaceFromRevitFace(Face geom,
            IEnumerable<PolyCurve> edgeLoops)
        {
            var dyFace = (dynamic)geom;
            return (Autodesk.DesignScript.Geometry.Surface)SurfaceExtractor.ExtractSurface(dyFace, edgeLoops);
        }

        public static List<PolyCurve> GetEdgeLoopsFromRevitFaceAsPolyCurves(Autodesk.Revit.DB.Face face)
        {
            return face.EdgeLoops.Cast<EdgeArray>()
                .Select(x => x.Cast<Autodesk.Revit.DB.Edge>())
                .Select(x => x.Select(t => t.AsCurveFollowingFace(face).ToProtoType(false)))
                .Select(PolyCurve.ByJoinedCurves)
                .ToList();
        }

    }

#endif
}
