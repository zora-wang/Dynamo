using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using Dynamo.DSEngine;
using Dynamo.UI.Commands;
using Dynamo.Utilities;
using HelixToolkit.Wpf;
using HelixToolkit.Wpf.SharpDX;
using SharpDX;
using Camera = HelixToolkit.Wpf.SharpDX.Camera;
using Color = System.Windows.Media.Color;
using Material = HelixToolkit.Wpf.SharpDX.Material;
using MeshBuilder = HelixToolkit.Wpf.SharpDX.MeshBuilder;
using MeshGeometry3D = HelixToolkit.Wpf.SharpDX.MeshGeometry3D;
using PerspectiveCamera = HelixToolkit.Wpf.SharpDX.PerspectiveCamera;

namespace Dynamo.ViewModels
{
    class Watch3DViewModel: INotifyPropertyChanged, IWatchViewModel
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private string _id = "";

        public ThreadSafeList<Point3D> _pointsCache = new ThreadSafeList<Point3D>();
        public ThreadSafeList<Point3D> _pointsCacheSelected = new ThreadSafeList<Point3D>();
        private ThreadSafeList<BillboardTextItem> _text = new ThreadSafeList<BillboardTextItem>();
        private Camera _camera;
        private bool _watchIsResizable;
        private Vector3D _upAxis = new Vector3D(0, 0, 1);

        public PhongMaterial MeshMaterial { get; private set; }
        public PhongMaterial MeshSelectedMaterial { get; private set; }
        public RenderTechnique RenderTechnique { get; private set; }
        public Vector3 DirectionalLightDirection { get; private set; }
        public Color4 DirectionalLightColor { get; private set; }
        public Color4 AmbientLightColor { get; private set; }

        public Camera Camera
        {
            get { return _camera; }
            private set
            {
                _camera = value;
                NotifyPropertyChanged("Camera");
            }
        }

        public System.Windows.Media.Media3D.Material HelixMeshMaterial
        {
            get { return Materials.White; }
        }

        public LineGeometry3D HelixGrid { get; set; }
        public LineGeometry3D HelixLines { get; set; }
        public LineGeometry3D HelixXAxes { get; set; }
        public LineGeometry3D HelixYAxes { get; set; }
        public LineGeometry3D HelixZAxes { get; set; }
        public MeshGeometry3D HelixMesh { get; set; }
        public LineGeometry3D HelixLinesSelected { get; set; }
        public MeshGeometry3D HelixMeshSelected { get; set; }
        public Transform3D Model1Transform { get; private set; }

        public ThreadSafeList<Point3D> HelixPoints
        {
            get { return _pointsCache; }
            set
            {
                _pointsCache = value;
                //NotifyPropertyChanged("HelixPoints");
            }
        }

        public ThreadSafeList<Point3D> HelixPointsSelected
        {
            get { return _pointsCacheSelected; }
            set
            {
                _pointsCacheSelected = value;
                //NotifyPropertyChanged("HelixPointsSelected");
            }
        }

        public ThreadSafeList<BillboardTextItem> HelixText
        {
            get
            {
                return _text;
            }
            set
            {
                _text = value;
                NotifyPropertyChanged("HelixText");
            }
        }

        public Vector3D UpAxis
        {
            get { return _upAxis; }
            set
            {
                _upAxis = value;
                NotifyPropertyChanged("UpAxis");
            }
        }

        public DelegateCommand SelectVisualizationInViewCommand { get; set; }
        public DelegateCommand GetBranchVisualizationCommand { get; set; }

        public bool WatchIsResizable
        {
            get { return _watchIsResizable; }
            set
            {
                _watchIsResizable = value;
                NotifyPropertyChanged("WatchIsResizable");
            }
        }

        public Watch3DViewModel(string id, bool isResizable)
        {
            _id = id;
            WatchIsResizable = isResizable;

            SelectVisualizationInViewCommand = new DelegateCommand(SelectVisualizationInView, CanSelectVisualizationInView);
            GetBranchVisualizationCommand = new DelegateCommand(GetBranchVisualization, CanGetBranchVisualization);

            Model1Transform = Transform3D.Identity;

            MeshMaterial = PhongMaterials.Blue;
            MeshSelectedMaterial = PhongMaterials.Blue;

            RenderTechnique = Techniques.RenderPhong;

            AmbientLightColor = new Color4(0.1f, 0.1f, 0.1f, 1.0f);
            DirectionalLightColor = SharpDX.Color.White;
            DirectionalLightDirection = new Vector3(-2, -5, -2);
            Camera = new PerspectiveCamera
            {
                Position = new System.Windows.Media.Media3D.Point3D(10, 10, 10),
                LookDirection = new System.Windows.Media.Media3D.Vector3D(-1, -1, -1),
                UpDirection = new System.Windows.Media.Media3D.Vector3D(0, 0, 1),
                NearPlaneDistance = 0.1,
                FarPlaneDistance = 100
            };
        }

        /// <summary>
        /// Use the render description returned from the visualization manager to update the visuals.
        /// The visualization event arguments will contain a render description and an id representing 
        /// the associated node. Visualizations for the background preview will return an empty id.
        /// </summary>
        /// <param name="e"></param>
        internal void RenderDrawables(VisualizationEventArgs e)
        {
            //check the id, if the id is meant for another watch,
            //then ignore it
            if (e.Id != _id)
            {
                return;
            }

            var sw = new Stopwatch();
            sw.Start();

            var points = new ThreadSafeList<Point3D>();
            var pointsSelected = new ThreadSafeList<Point3D>();
            var text = new ThreadSafeList<BillboardTextItem>();

            var lines = new LineBuilder();
            var linesSelected = new LineBuilder();
            var redLines = new LineBuilder();
            var greenLines = new LineBuilder();
            var blueLines = new LineBuilder();
            var meshes = new MeshBuilder();
            var meshesSelected = new MeshBuilder();

            foreach (var package in e.Packages)
            {
                ConvertPoints(package, points, pointsSelected, text);
                ConvertLines(package, lines, linesSelected, redLines, greenLines, blueLines, text);
                ConvertMeshes(package, meshes, meshesSelected);
            }
            
            HelixPoints = points;
            HelixPointsSelected = pointsSelected;

            HelixLines = lines.ToLineGeometry3D();
            HelixLinesSelected = linesSelected.ToLineGeometry3D();
            HelixXAxes = redLines.ToLineGeometry3D();
            HelixYAxes = greenLines.ToLineGeometry3D();
            HelixZAxes = blueLines.ToLineGeometry3D();
            HelixMesh = meshes.ToMeshGeometry3D();
            HelixMeshSelected = meshesSelected.ToMeshGeometry3D();

            HelixText = text;

            HelixGrid = LineBuilder.GenerateGrid(Vector3.UnitZ, -10, 10);

            //var sb = new StringBuilder();
            //sb.AppendLine();
            //sb.AppendLine(string.Format("Rendering complete:"));
            //sb.AppendLine(string.Format("Points: {0}", rd.Points.Count + rd.SelectedPoints.Count));
            //sb.AppendLine(string.Format("Line segments: {0}", rd.Lines.Count / 2 + rd.SelectedLines.Count / 2));
            //sb.AppendLine(string.Format("Mesh vertices: {0}",
            //    rd.Meshes.SelectMany(x => x.Positions).Count() +
            //    rd.SelectedMeshes.SelectMany(x => x.Positions).Count()));
            //sb.Append(string.Format("Mesh faces: {0}",
            //    rd.Meshes.SelectMany(x => x.TriangleIndices).Count() / 3 +
            //    rd.SelectedMeshes.SelectMany(x => x.TriangleIndices).Count() / 3));
            ////DynamoLogger.Instance.Log(sb.ToString());
            //Debug.WriteLine(sb.ToString());

            sw.Stop();

            Debug.WriteLine(string.Format("{0} ellapsed for updating background preview.", sw.Elapsed));
        }

        private void ConvertPoints(RenderPackage p,
            ThreadSafeList<Point3D> points,
            ThreadSafeList<Point3D> pointsSelected,
            ThreadSafeList<BillboardTextItem> text)
        {
            for (int i = 0; i < p.PointVertices.Count; i += 3)
            {
                var pos = new Point3D(
                    p.PointVertices[i],
                    p.PointVertices[i + 1],
                    p.PointVertices[i + 2]);

                if (p.Selected)
                {
                    pointsSelected.Add(pos);
                }
                else
                {
                    points.Add(pos);
                }

                if (p.DisplayLabels)
                {
                    text.Add(new BillboardTextItem { Text = p.Tag, Position = pos });
                }
            }
        }

        private void ConvertLines(RenderPackage p,
            LineBuilder lines,
            LineBuilder linesSelected,
            LineBuilder redLines,
            LineBuilder greenLines,
            LineBuilder blueLines,
            ThreadSafeList<BillboardTextItem> text)
        {
            int colorCount = 0;
            for (int i = 0; i < p.LineStripVertices.Count - 3; i += 3)
            {
                //var start = new Point3D(
                //    p.LineStripVertices[i],
                //    p.LineStripVertices[i + 1],
                //    p.LineStripVertices[i + 2]);

                //var end = new Point3D(
                //    p.LineStripVertices[i + 3],
                //    p.LineStripVertices[i + 4],
                //    p.LineStripVertices[i + 5]);

                var start = new Vector3(
                    (float)p.LineStripVertices[i],
                    (float)p.LineStripVertices[i + 1],
                    (float)p.LineStripVertices[i + 2]);

                var end = new Vector3(
                    (float)p.LineStripVertices[i + 3],
                    (float)p.LineStripVertices[i + 4],
                    (float)p.LineStripVertices[i + 5]);

                //HACK: test for line color using only 
                //the start value
                var startColor = System.Windows.Media.Color.FromRgb(
                    p.LineStripVertexColors[colorCount],
                    p.LineStripVertexColors[colorCount + 1],
                    p.LineStripVertexColors[colorCount + 2]);

                //var endColor = new Point3D(
                //    p.LineStripVertexColors[i + 3],
                //    p.LineStripVertexColors[i + 4],
                //    p.LineStripVertexColors[i + 5]);

                //draw a label at the start of the curve
                if (p.DisplayLabels && i == 0)
                {
                    text.Add(new BillboardTextItem { Text = p.Tag, Position = new Point3D(start.X, start.Y, start.Z) });
                }

                bool isAxis = false;
                if (startColor == System.Windows.Media.Color.FromRgb(255, 0, 0))
                {
                    //redLines.Add(start);
                    //redLines.Add(end);
                    redLines.AddLine(start, end);
                    isAxis = true;
                }
                else if (startColor == System.Windows.Media.Color.FromRgb(0, 255, 0))
                {
                    //greenLines.Add(start);
                    //greenLines.Add(end);
                    greenLines.AddLine(start, end);
                    isAxis = true;
                }
                else if (startColor == Color.FromRgb(0, 0, 255))
                {
                    //blueLines.Add(start);
                    //blueLines.Add(end);
                    blueLines.AddLine(start, end);
                    isAxis = true;
                }

                if (!isAxis)
                {
                    if (p.Selected)
                    {
                        //linesSelected.Add(start);
                        //linesSelected.Add(end);
                        linesSelected.AddLine(start, end);
                    }
                    else
                    {
                        //lines.Add(start);
                        //lines.Add(end);
                        lines.AddLine(start, end);
                    }
                }

                colorCount += 4;
            }
        }

        private void ConvertMeshes(RenderPackage p,
            MeshBuilder meshes,
            MeshBuilder meshesSelected)
        {
            //var sw = new Stopwatch();
            //sw.Start();
            
            //var builder = new MeshBuilder();
            var points = new List<Vector3>();
            var tex = new List<Vector2>();
            var norms = new List<Vector3>();
            var tris = new List<int>();

            for (int i = 0; i < p.TriangleVertices.Count; i += 3)
            {
                //var new_point = new Point3D(p.TriangleVertices[i],
                //                            p.TriangleVertices[i + 1],
                //                            p.TriangleVertices[i + 2]);

                //var normal = new Vector3D(p.TriangleNormals[i],
                //                            p.TriangleNormals[i + 1],
                //                            p.TriangleNormals[i + 2]);

                var new_point = new Vector3((float)p.TriangleVertices[i],
                                            (float)p.TriangleVertices[i + 1],
                                            (float)p.TriangleVertices[i + 2]);

                var normal = new Vector3((float)p.TriangleNormals[i],
                                            (float)p.TriangleNormals[i + 1],
                                            (float)p.TriangleNormals[i + 2]);
                
                //find a matching point
                //compare the angle between the normals
                //to discern a 'break' angle for adjacent faces
                //int foundIndex = -1;
                //for (int j = 0; j < points.Count; j++)
                //{
                //    var testPt = points[j];
                //    var testNorm = norms[j];
                //    var ang = Vector3D.AngleBetween(normal, testNorm);

                //    if (new_point.X == testPt.X &&
                //        new_point.Y == testPt.Y &&
                //        new_point.Z == testPt.Z &&
                //        ang > 90.0000)
                //    {
                //        foundIndex = j;
                //        break;
                //    }
                //}

                //if (foundIndex != -1)
                //{
                //    tris.Add(foundIndex);
                //    continue;
                //}

                tris.Add(points.Count);
                points.Add(new_point);
                norms.Add(normal);
                tex.Add(new Vector2(0.0f, 0.0f));

                //octree.AddNode(new_point.X, new_point.Y, new_point.Z, node.GUID.ToString());
            }

            if (points.Count > 0)
            {
                if (p.Selected)
                    meshesSelected.Append(points, tris, norms, tex);
                else
                    meshes.Append(points, tris, norms, tex);
            }


            //don't add empty meshes
            //if (builder.Positions.Count > 0)
            //{
            //    if (p.Selected)
            //    {
            //        meshesSelected.Add(builder.ToMesh(true));
            //    }
            //    else
            //    {
            //        meshes.Add(builder.ToMesh(true));
            //    }
            //}
        }

        #region IWatchViewModel interface

        internal void SelectVisualizationInView(object parameters)
        {
            Debug.WriteLine("Selecting mesh from background watch.");

            var arr = (double[])parameters;
            double x = arr[0];
            double y = arr[1];
            double z = arr[2];

            dynSettings.Controller.VisualizationManager.LookupSelectedElement(x, y, z);
        }

        internal bool CanSelectVisualizationInView(object parameters)
        {
            if (parameters != null)
            {
                return true;
            }

            return false;
        }

        public void GetBranchVisualization(object parameters)
        {
            dynSettings.Controller.VisualizationManager.RenderUpstream(_id);
        }

        public bool CanGetBranchVisualization(object parameter)
        {
            if (!string.IsNullOrEmpty(_id))
            {
                return true;
            }

            if (dynSettings.Controller.DynamoViewModel.FullscreenWatchShowing)
            {
                return true;
            }

            return false;
        }

        #endregion
    }
}
