using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Analysis.DataTypes;

using Autodesk.DesignScript.Geometry;
using Autodesk.DesignScript.Interfaces;
using Autodesk.DesignScript.Runtime;

using DSCore;

using Math = System.Math;
using Point = Autodesk.DesignScript.Geometry.Point;

namespace Analysis
{
    public class ColoredSurface : IGraphicItem
    {
        private Surface surface;
        private DSCore.Color[] colors;
        private UV[] uvs;

        private ColoredSurface(Surface surface, DSCore.Color[] colors, UV[] uvs)
        {
            this.surface = surface;
            this.colors = colors;
            this.uvs = uvs;
        }

        public static ColoredSurface ByColorsAndUVs(Surface surface, DSCore.Color[] colors, UV[] uvs)
        {
            if (surface == null)
            {
                throw new ArgumentNullException("surface");
            }

            if (colors == null)
            {
                throw new ArgumentNullException("colors");
            }

            if (!colors.Any())
            {
                throw new ArgumentException("There are no colors specified.");
            }

            if (uvs == null)
            {
                throw new ArgumentNullException("uvs");
            }

            if (!uvs.Any())
            {
                throw new ArgumentException("There are no UVs specified.");
            }

            if (uvs.Count() != colors.Count())
            {
                throw new Exception("The number of colors and the number of locations specified must be equal.");
            }

            return new ColoredSurface(surface, colors, uvs);
        }

        public void Tessellate(IRenderPackage package, double tol = -1, int maxGridLines = 512)
        {
            // As we write the vertex locations to the render package,
            // calculate the UV location for the vertex and 
            surface.Tessellate(package);

            var colorCount = 0;

            for (int i = 0; i < package.TriangleVertices.Count; i += 3)
            {
                var vx = package.TriangleVertices[i];
                var vy = package.TriangleVertices[i + 1];
                var vz = package.TriangleVertices[i + 2];

                // Get the triangle vertex
                var v = Point.ByCoordinates(vx, vy, vz);

                var an = package.TriangleNormals[i];
                var bn = package.TriangleNormals[i + 1];
                var cn = package.TriangleNormals[i + 2];

                var norm = Vector.ByCoordinates(an, bn, cn);
                var xsects = surface.ProjectInputOnto(v, norm);

                if (!xsects.Any()) continue;

                // The parameter at the triangle vertex
                var vUV = surface.UVParameterAtPoint(xsects.First() as Point);

                // The distances from this to each of the calculation points
                var distances = new double[uvs.Count()];
                for (int k=0; k<uvs.Count(); k++)
                {
                    var uv = uvs[k];
                    var d = Math.Sqrt(Math.Pow(uv.U - vUV.U, 2) + Math.Pow(uv.V - vUV.V, 2));
                    distances[k] = d;
                }

                // Calculate the averages of all 
                // color components
                var a = 0.0;
                var r = 0.0;
                var g = 0.0;
                var b = 0.0;

                var totalWeight = 0.0;

                for (int j = 0; j < colors.Count(); j++)
                {
                    var c = colors[j];
                    var d = distances[j];

                    a += c.Alpha * d;
                    r += c.Red * d;
                    g += c.Green * d;
                    b += c.Blue * d;

                    totalWeight += d;
                }

                var totalR = (byte)(r/totalWeight);
                var totalG = (byte)(g/totalWeight);
                var totalB = (byte)(b/totalWeight);
                var totalA = (byte)(a/totalWeight);

                package.TriangleVertexColors[colorCount] = totalR;
                package.TriangleVertexColors[colorCount + 1] = totalG;
                package.TriangleVertexColors[colorCount + 2] = totalB;
                package.TriangleVertexColors[colorCount + 3] = totalA;

                Debug.WriteLine(string.Format("v:{0}, uv:{1}, c:{2}", v, vUV, Color.ByARGB(totalA, totalR, totalG, totalB)));

                colorCount += 4;
            }
        }
    }

    [IsVisibleInDynamoLibrary(false)]
    public class ColoredSurfacePackage : IRenderPackage
    {
        internal ColoredSurfacePackage()
        {
            LineStripVertices = new List<double>();
            PointVertices = new List<double>();
            TriangleVertices = new List<double>();
            TriangleNormals = new List<double>();
            PointVertexColors = new List<byte>();
            LineStripVertexColors = new List<byte>();
            TriangleVertexColors = new List<byte>();
            LineStripVertexCounts = new List<int>();
        }

        public void PushPointVertex(double x, double y, double z)
        {
            PointVertices.Add(x);
            PointVertices.Add(y);
            PointVertices.Add(z);
        }

        public void PushPointVertexColor(byte red, byte green, byte blue, byte alpha)
        {
            PointVertexColors.Add(red);
            PointVertexColors.Add(green);
            PointVertexColors.Add(blue);
            PointVertexColors.Add(alpha);
        }

        public void PushTriangleVertex(double x, double y, double z)
        {
            TriangleVertices.Add(x);
            TriangleVertices.Add(y);
            TriangleVertices.Add(z);
        }

        public void PushTriangleVertexNormal(double x, double y, double z)
        {
            TriangleNormals.Add(x);
            TriangleNormals.Add(y);
            TriangleNormals.Add(z);
        }

        public void PushTriangleVertexColor(byte red, byte green, byte blue, byte alpha)
        {
            TriangleVertexColors.Add(red);
            TriangleVertexColors.Add(green);
            TriangleVertexColors.Add(blue);
            TriangleVertexColors.Add(alpha);
        }

        public void PushLineStripVertex(double x, double y, double z)
        {
            LineStripVertices.Add(x);
            LineStripVertices.Add(y);
            LineStripVertices.Add(z);
        }

        public void PushLineStripVertexCount(int n)
        {
            LineStripVertexCounts.Add(n);
        }

        public void PushLineStripVertexColor(byte red, byte green, byte blue, byte alpha)
        {
            LineStripVertexColors.Add(red);
            LineStripVertexColors.Add(green);
            LineStripVertexColors.Add(blue);
            LineStripVertexColors.Add(alpha);
        }

        public void Clear()
        {
            LineStripVertices.Clear();
            PointVertices.Clear();
            TriangleVertices.Clear();
            TriangleNormals.Clear();
            PointVertexColors.Clear();
            LineStripVertexColors.Clear();
            TriangleVertexColors.Clear();
            LineStripVertexCounts.Clear();
        }

        public List<double> LineStripVertices { get; set; }
        public List<double> PointVertices { get; set; }
        public List<double> TriangleVertices { get; set; }
        public List<double> TriangleNormals { get; set; }
        public List<byte> PointVertexColors { get; set; }
        public List<byte> LineStripVertexColors { get; set; }
        public List<byte> TriangleVertexColors { get; set; }
        public List<int> LineStripVertexCounts { get; set; }
        public string Tag { get; set; }
        public IntPtr NativeRenderPackage { get; private set; }
    }
}
