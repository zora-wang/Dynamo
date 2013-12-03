using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Autodesk.Revit.DB;
using Dynamo.Models;
using Dynamo.Revit;
using Dynamo.Utilities;
using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics.LinearAlgebra.Generic;
using Microsoft.FSharp.Collections;

namespace Dynamo.Nodes
{
    [NodeName("Line by Endpoints")]
    [NodeCategory(BuiltinNodeCategories.GEOMETRY_CURVE_CREATE)]
    [NodeDescription("Creates a geometric line.")]
    public class LineBound : GeometryBase
    {
        public LineBound()
        {
            InPortData.Add(new PortData("start", "Start XYZ", typeof(FScheme.Value.Container)));
            InPortData.Add(new PortData("end", "End XYZ", typeof(FScheme.Value.Container)));
            //InPortData.Add(new PortData("bound?", "Boolean: Is this line bounded?", typeof(bool)));

            OutPortData.Add(new PortData("line", "Line", typeof(FScheme.Value.Container)));

            RegisterAllPorts();
        }

        public override FScheme.Value Evaluate(FSharpList<FScheme.Value> args)
        {
            var ptA = ((FScheme.Value.Container)args[0]).Item;
            var ptB = ((FScheme.Value.Container)args[1]).Item;

            Line line = null;

            if (ptA is XYZ)
            {

                line = dynRevitSettings.Doc.Application.Application.Create.NewLineBound(
                  (XYZ)ptA, (XYZ)ptB
                  );


            }
            else if (ptA is ReferencePoint)
            {
                line = dynRevitSettings.Doc.Application.Application.Create.NewLineBound(
                  (XYZ)((ReferencePoint)ptA).Position, (XYZ)((ReferencePoint)ptB).Position
               );

            }

            return FScheme.Value.NewContainer(line);
        }
    }

    [NodeName("Line By Start Point Direction Length")]
    [NodeCategory(BuiltinNodeCategories.GEOMETRY_CURVE_CREATE)]
    [NodeDescription("Creates a geometric line from a start point, a direction, and a length.")]
    public class LineByStartPtDirLength : GeometryBase
    {
        public LineByStartPtDirLength()
        {
            InPortData.Add(new PortData("start", "The origin of the line.", typeof(FScheme.Value.Container)));
            InPortData.Add(new PortData("direction", "The direction vector.", typeof(FScheme.Value.Container)));
            InPortData.Add(new PortData("length", "The length of the line.", typeof(FScheme.Value.Container)));

            OutPortData.Add(new PortData("line", "Line", typeof(FScheme.Value.Container)));

            RegisterAllPorts();

            ArgumentLacing = LacingStrategy.Longest;
        }

        public override FScheme.Value Evaluate(FSharpList<FScheme.Value> args)
        {
            var ptA = (XYZ)((FScheme.Value.Container)args[0]).Item;
            var vec = (XYZ)((FScheme.Value.Container)args[1]).Item;
            var len = ((FScheme.Value.Number)args[2]).Item;

            if (len == 0)
            {
                throw new Exception("Cannot create a line with zero length.");
            }

            var ptB = ptA + vec.Multiply(len);

            if (ptA.IsAlmostEqualTo(ptB))
            {
                throw new Exception("The start point and end point are extremely close together. The line will be too short.");
            }

            var line = dynRevitSettings.Doc.Application.Application.Create.NewLineBound(ptA, ptB);

            return FScheme.Value.NewContainer(line);
        }
    }

    [NodeName("Line by Origin Direction")]
    [NodeCategory(BuiltinNodeCategories.GEOMETRY_CURVE_CREATE)]
    [NodeDescription("Creates a line in the direction of an XYZ normal.")]
    public class LineVectorfromXyz : NodeWithOneOutput
    {
        public LineVectorfromXyz()
        {
            InPortData.Add(new PortData("normal", "Normal Point (XYZ)", typeof(FScheme.Value.Container)));
            InPortData.Add(new PortData("origin", "Origin Point (XYZ)", typeof(FScheme.Value.Container)));
            OutPortData.Add(new PortData("curve", "Curve", typeof(FScheme.Value.Container)));

            RegisterAllPorts();
        }

        public override FScheme.Value Evaluate(FSharpList<FScheme.Value> args)
        {
            var ptA = (XYZ)((FScheme.Value.Container)args[0]).Item;
            var ptB = (XYZ)((FScheme.Value.Container)args[1]).Item;

            // CurveElement c = MakeLine(this.UIDocument.Document, ptA, ptB);
            CurveElement c = MakeLineCBP(dynRevitSettings.Doc.Document, ptA, ptB);

            return FScheme.Value.NewContainer(c);
        }

        public Autodesk.Revit.DB.ModelCurve MakeLine(Document doc, XYZ ptA, XYZ ptB)
        {
            Autodesk.Revit.ApplicationServices.Application app = doc.Application;
            // Create plane by the points
            Line line = app.Create.NewLine(ptA, ptB, true);
            XYZ norm = ptA.CrossProduct(ptB);
            double length = norm.GetLength();
            if (length == 0) norm = XYZ.BasisZ;
            Autodesk.Revit.DB.Plane plane = app.Create.NewPlane(norm, ptB);
            Autodesk.Revit.DB.SketchPlane skplane = doc.FamilyCreate.NewSketchPlane(plane);
            // Create line here
            Autodesk.Revit.DB.ModelCurve modelcurve = doc.FamilyCreate.NewModelCurve(line, skplane);
            return modelcurve;
        }

        public Autodesk.Revit.DB.CurveByPoints MakeLineCBP(Document doc, XYZ ptA, XYZ ptB)
        {
            ReferencePoint sunRP = doc.FamilyCreate.NewReferencePoint(ptA);
            ReferencePoint originRP = doc.FamilyCreate.NewReferencePoint(ptB);
            ReferencePointArray sunRPArray = new ReferencePointArray();
            sunRPArray.Append(sunRP);
            sunRPArray.Append(originRP);
            Autodesk.Revit.DB.CurveByPoints sunPath = doc.FamilyCreate.NewCurveByPoints(sunRPArray);
            return sunPath;
        }
    }

    [NodeName("Bisector Line")]
    [NodeCategory(BuiltinNodeCategories.REVIT_REFERENCE)]
    [NodeDescription("Creates bisector of two lines")]
    [DoNotLoadOnPlatforms(Context.REVIT_2013, Context.REVIT_2014, Context.VASARI_2013)]
    public class Bisector : RevitTransactionNodeWithOneOutput
    {
        public Bisector()
        {
            InPortData.Add(new PortData("line1", "First Line", typeof(FScheme.Value.Container)));
            InPortData.Add(new PortData("line2", "Second Line", typeof(FScheme.Value.Container)));
            OutPortData.Add(new PortData("bisector", "Bisector Line", typeof(FScheme.Value.Container)));

            RegisterAllPorts();
        }
        public override FScheme.Value Evaluate(FSharpList<FScheme.Value> args)
        {
            Line line1 = (Line)((FScheme.Value.Container)args[0]).Item;
            Line line2 = (Line)((FScheme.Value.Container)args[1]).Item;

            Type LineType = typeof(Autodesk.Revit.DB.Line);

            MethodInfo[] lineInstanceMethods = LineType.GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);

            System.String nameOfMethodCreateBisector = "CreateBisector";
            Line result = null;

            foreach (MethodInfo m in lineInstanceMethods)
            {
                if (m.Name == nameOfMethodCreateBisector)
                {
                    object[] argsM = new object[1];
                    argsM[0] = line2;

                    result = (Line)m.Invoke(line1, argsM);

                    break;
                }
            }

            return FScheme.Value.NewContainer(result);
        }
    }

    [NodeName("Best Fit Line")]
    [NodeCategory(BuiltinNodeCategories.GEOMETRY_CURVE_FIT)]
    [NodeDescription("Determine the best fit line for a set of points.  This line minimizes the sum of the distances between the line and the point set.")]
    internal class BestFitLine : NodeModel
    {
        private readonly PortData _axisPort = new PortData(
            "axis", "A normalized vector representing the axis of the best fit line.",
            typeof(FScheme.Value.Container));

        private readonly PortData _avgPort = new PortData(
            "avg", "The average (mean) of the point list.", typeof(FScheme.Value.Container));

        public BestFitLine()
        {
            InPortData.Add(new PortData("XYZs", "A List of XYZ's.", typeof(FScheme.Value.List)));
            OutPortData.Add(_axisPort);
            OutPortData.Add(_avgPort);

            ArgumentLacing = LacingStrategy.Longest;
            RegisterAllPorts();
        }

        public static List<T> AsGenericList<T>(FSharpList<FScheme.Value> list)
        {
            return list.Cast<FScheme.Value.Container>().Select(x => x.Item).Cast<T>().ToList();
        }

        public static XYZ MeanXYZ(List<XYZ> pts)
        {
            return pts.Aggregate(new XYZ(), (i, p) => i.Add(p)).Divide(pts.Count);
        }

        public static XYZ MakeXYZ(Vector<double> vec)
        {
            return new XYZ(vec[0], vec[1], vec[2]);
        }

        public static void PrincipalComponentsAnalysis(List<XYZ> pts, out XYZ meanXYZ, out List<XYZ> orderEigenvectors)
        {
            var meanPt = MeanXYZ(pts);
            meanXYZ = meanPt;

            var l = pts.Count();
            var ctrdMat = DenseMatrix.Create(3, l, (r, c) => pts[c][r] - meanPt[r]);
            var covarMat = (1 / ((double)pts.Count - 1)) * ctrdMat * ctrdMat.Transpose();

            var eigen = covarMat.Evd();

            var valPairs = new List<Tuple<double, Vector<double>>>
                {
                    new Tuple<double, Vector<double>>(eigen.EigenValues()[0].Real, eigen.EigenVectors().Column(0)),
                    new Tuple<double, Vector<double>>(eigen.EigenValues()[1].Real, eigen.EigenVectors().Column(1)),
                    new Tuple<double, Vector<double>>(eigen.EigenValues()[2].Real, eigen.EigenVectors().Column(2))
                };

            var sortEigVecs = valPairs.OrderByDescending((x) => x.Item1).ToList();

            orderEigenvectors = new List<XYZ>
                {
                    MakeXYZ( sortEigVecs[0].Item2 ),
                    MakeXYZ( sortEigVecs[1].Item2 ),
                    MakeXYZ( sortEigVecs[2].Item2 )
                };
        }

        public override void Evaluate(FSharpList<FScheme.Value> args, Dictionary<PortData, FScheme.Value> outPuts)
        {
            var pts = ((FScheme.Value.List)args[0]).Item;

            var ptList = AsGenericList<XYZ>(pts);
            XYZ meanPt;
            List<XYZ> orderedEigenvectors;
            PrincipalComponentsAnalysis(ptList, out meanPt, out orderedEigenvectors);

            outPuts[_axisPort] = FScheme.Value.NewContainer(orderedEigenvectors[0]);
            outPuts[_avgPort] = FScheme.Value.NewContainer(meanPt);
        }
    }
    [NodeName("Line pattern along Line")]
    [NodeCategory(BuiltinNodeCategories.GEOMETRY_CURVE_CREATE)]
    [NodeDescription("Generate pattern of lines along given Line.")]
    public class LinePattern : NodeWithOneOutput
    {
        static Line linePlacementDefault = null;
        public LinePattern()
        {
            ArgumentLacing = LacingStrategy.Longest;

            InPortData.Add(new PortData("Line", "The Line to pattern along.", typeof(FScheme.Value.Container)));
            InPortData.Add(new PortData("Increments", "The pattern controls. Negative for intervals without line placed.", typeof(FScheme.Value.List)));
            InPortData.Add(new PortData("Placement line", "Controls positioning of pattern ", typeof(FScheme.Value.Container), FScheme.Value.NewContainer(linePlacementDefault)));
            OutPortData.Add(new PortData("Pattern Lines", "The lines pattern along the input line via pattern.", typeof(FScheme.Value.List)));

            RegisterAllPorts();
        }

        public override FScheme.Value Evaluate(FSharpList<FScheme.Value> args)
        {
            var line = ((FScheme.Value.Container)args[0]).Item as Line;

            List<double> increments = new List<double>();
            var input = (args[1] as FScheme.Value.List).Item;

            foreach (FScheme.Value v in input)
            {
                double oneInc = (double)((FScheme.Value.Number)v).Item;
                increments.Add(oneInc);
            }
            
            var startParam = line.get_EndParameter(0);
            var endParam = line.get_EndParameter(1);
            double patternParam = startParam;

            var placementLine = (Line)((FScheme.Value.Container)args[2]).Item;
            if (placementLine != null)
            {
                //count total pattern length
                double patternPeriod = 0.0;
                for (int ii = 0; ii < increments.Count; ii++)
                {
                    patternPeriod += Math.Abs(increments[ii]);
                }
                if (patternPeriod < 1.0e-9)
                    throw new Exception("Cannot create line patterns with zero pattern length."); 
                //intersect line with placement line
                Line lineUnbound = (Line)line.Clone();
                lineUnbound.MakeUnbound();

                Line linePlacementUnbound = (Line)placementLine.Clone();
                linePlacementUnbound.MakeUnbound();

                //intersect
                IntersectionResultArray intArray;
                lineUnbound.Intersect(linePlacementUnbound, out intArray);
                if (intArray == null || intArray.Size != 1)
                    throw new Exception("Could not intersect placement line with pattern holding line");
                XYZ patternPoint = intArray.get_Item(0).XYZPoint;
                patternParam = lineUnbound.Project(patternPoint).Parameter;
                while (patternParam > startParam + 1.0e-9)
                    patternParam -= patternPeriod;
            }

            double shortCurve = dynRevitSettings.Revit.Application.ShortCurveTolerance;

            List<Line> patternedLines = new List<Line>();
            double sPar = patternParam;
            double ePar = patternParam;
            int indexPattern = 0;

            for (; sPar < endParam - 1.0e-9; indexPattern = (indexPattern + 1) % (increments.Count), sPar = ePar)
            {
                ePar = sPar +  Math.Abs(increments[indexPattern]);
                if (ePar > endParam)
                    ePar = endParam;
                if (increments[indexPattern] < 1.0e-9 || ePar < startParam + 1.0e-9)
                    continue;
                if (sPar < startParam)
                    sPar = startParam;
                XYZ startXYZ = line.Evaluate(sPar, false);
                XYZ endXYZ = line.Evaluate(ePar, false);
                if (startXYZ.DistanceTo(endXYZ) < shortCurve + 1.0e-9)
                    continue;
                Line line_ = Line.CreateBound(startXYZ, endXYZ);
                patternedLines.Add(line_);
            }
            var result = FSharpList<FScheme.Value>.Empty;

            for (int indexLine = patternedLines.Count - 1; indexLine > -1; indexLine--)
            {
                result = FSharpList<FScheme.Value>.Cons(FScheme.Value.NewContainer(patternedLines[indexLine]), result);
            }

            return FScheme.Value.NewList(result);
        }
    }

    [NodeName("Line pattern along Face")]
    [NodeCategory(BuiltinNodeCategories.GEOMETRY_CURVE_CREATE)]
    [NodeDescription("Generate pattern of lines along given Line.")]
    public class LinePatternAlongFace : NodeWithOneOutput
    {
        public LinePatternAlongFace()
        {
            ArgumentLacing = LacingStrategy.Longest;

            InPortData.Add(new PortData("Line", "The Placement Line to pattern to match and be parallel.", typeof(FScheme.Value.Container)));
            InPortData.Add(new PortData("Face", "The planar face to contain patterned lines.", typeof(FScheme.Value.Container)));
            InPortData.Add(new PortData("Increments", "The pattern controls. Negative for intervals without line placed.", typeof(FScheme.Value.List)));
            OutPortData.Add(new PortData("Pattern Lines", "The lines pattern along the input line via pattern.", typeof(FScheme.Value.List)));

            RegisterAllPorts();
        }

        public override FScheme.Value Evaluate(FSharpList<FScheme.Value> args)
        {
            var lineIn = ((FScheme.Value.Container)args[0]).Item as Line;

            List<double> increments = new List<double>();
            var input = (args[2] as FScheme.Value.List).Item;

            foreach (FScheme.Value v in input)
            {
                double oneInc = (double)((FScheme.Value.Number)v).Item;
                increments.Add(oneInc);
            }

            var face = ((FScheme.Value.Container)args[1]).Item as Face;
            if (!(face is PlanarFace))
                throw new Exception("Cannot create line patterns on non-planar face.");

            PlanarFace planarFace = face as PlanarFace;
            XYZ norm = planarFace.Normal;

            XYZ lineEnd0 = lineIn.get_EndPoint(0);
            XYZ lineEnd1 = lineIn.get_EndPoint(1);

            XYZ projectS = lineEnd0 - norm.Multiply((lineEnd0-planarFace.Origin).DotProduct(norm));
            XYZ projectE = lineEnd1 - norm.Multiply((lineEnd1-planarFace.Origin).DotProduct(norm));

            Line line = Line.CreateBound(projectS, projectE);

            BoundingBoxUV box = face.GetBoundingBox();

            XYZ incrementVec = norm.CrossProduct(line.Direction).Normalize();

            UV uv_min = box.Min;
            UV uv_max = box.Max;

            double minPattern = 0.0;
            double maxPattern = 0.0;

            double minExt = 0.0;
            double maxExt = 0.0;

            for (int ii = 0; ii < 2; ii++)
            {
                for (int jj = 0; jj < 2; jj++)
                {
                    UV uvs = new UV(ii == 0 ? uv_min.U : uv_max.U, jj == 0 ? uv_min.V : uv_max.V);
                    XYZ cornerXYZ = planarFace.Evaluate(uvs);
                    double val = (cornerXYZ - line.Origin).DotProduct(incrementVec);
                    if (ii == 0 && jj == 0)
                    {
                        minPattern = val;
                        maxPattern = val;
                        minExt = (cornerXYZ - line.Origin).DotProduct(line.Direction);
                        maxExt = (cornerXYZ - line.Origin).DotProduct(line.Direction);
                    }
                    else 
                    {
                        if (minPattern > val)
                            minPattern = val;
                        if (maxPattern < val)
                            maxPattern = val;
                        if (minExt > (cornerXYZ - line.Origin).DotProduct(line.Direction))
                            minExt = (cornerXYZ - line.Origin).DotProduct(line.Direction);
                        if (maxExt < (cornerXYZ - line.Origin).DotProduct(line.Direction))
                            maxExt = (cornerXYZ - line.Origin).DotProduct(line.Direction);
                    }

                }
            }

            double patternPeriod = 0.0;
            for (int ii = 0; ii < increments.Count; ii++)
            {
                patternPeriod += Math.Abs(increments[ii]);
            }
            if (patternPeriod < 1.0e-9)
                throw new Exception("Cannot create line patterns with zero pattern length.");

            var locPointMin = line.Origin;

            while ((locPointMin-line.Origin).DotProduct(incrementVec) > minPattern - 1.0e-9)
                locPointMin = locPointMin - incrementVec.Multiply(patternPeriod);

            var locPointMax = line.Origin;

            while ((locPointMax-line.Origin).DotProduct(incrementVec) < maxPattern - 1.0e-9)
                locPointMax = locPointMax + incrementVec.Multiply(patternPeriod);

            //ready to make pattern!!

            double startPar = (locPointMin - line.Origin).DotProduct(incrementVec);
            double endPar = (locPointMax - line.Origin).DotProduct(incrementVec);
     
            List<Line> patternedLines = new List<Line>();

            int indexPattern = 0;

            double curPar = startPar;
            double nextPar = startPar;

            for (; curPar < endPar + 1.0e-9; indexPattern = (indexPattern + 1) % (increments.Count), curPar = nextPar)
            {
                nextPar = curPar + Math.Abs(increments[indexPattern]);
                if (curPar > endPar + 1.0e-9)
                    break;
                else if (nextPar < endPar + 1.0e-9 && nextPar > endPar - 1.0e-9)
                    nextPar = endPar;
                if (increments[indexPattern] < 1.0e-9 || curPar < startPar - 1.0e-9)
                    continue;
                XYZ originLine = line.Origin + incrementVec.Multiply(curPar);
                Line lineForInt = Line.CreateUnbound(originLine, line.Direction);
                
                lineForInt.MakeBound(minExt, maxExt);

                Autodesk.Revit.DB.Plane plane = new Autodesk.Revit.DB.Plane(incrementVec, originLine);

                Face faceForInt = CurveFaceIntersection.buildFaceOnPlaneByCurveExtensions(lineForInt, plane);

                //IntersectionResultArray intResults = null;
                Curve curveInt;
                FaceIntersectionFaceResult result_ = face.Intersect(faceForInt, out curveInt);
                if (curveInt == null || result_ == FaceIntersectionFaceResult.NonIntersecting)
                    continue;
                Line intLine = curveInt as Line;
                //more checks
                XYZ intEnd0 = intLine.get_EndPoint(0);
                XYZ intEnd1 = intLine.get_EndPoint(1);

                if (planarFace.Project(intEnd0) == null || planarFace.Project(intEnd1) == null)
                    continue;
 
                patternedLines.Add(curveInt as Line);
                /*
                for (int ii = 0; ii < intResults.Size - 1; ii += 2)
                {

                    Line line_ = Line.CreateBound(intResults.get_Item(ii).XYZPoint, intResults.get_Item(ii+1).XYZPoint);
                    patternedLines.Add(line_);
                }
                */
            }
            var result = FSharpList<FScheme.Value>.Empty;

            for (int indexLine = patternedLines.Count - 1; indexLine > -1; indexLine--)
            {
                result = FSharpList<FScheme.Value>.Cons(FScheme.Value.NewContainer(patternedLines[indexLine]), result);
            }

            return FScheme.Value.NewList(result);
        }
    }
}
