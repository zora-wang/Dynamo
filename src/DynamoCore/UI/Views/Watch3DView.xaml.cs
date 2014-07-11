using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Threading;

using Autodesk.DesignScript.Geometry;

using Dynamo.DSEngine;
using Dynamo.Utilities;
using Dynamo.ViewModels;

using DynamoUtilities;

using HelixToolkit.Wpf;
using Color = System.Windows.Media.Color;
using MenuItem = System.Windows.Controls.MenuItem;
using PixelFormat = System.Drawing.Imaging.PixelFormat;
using Point = System.Windows.Point;
using UserControl = System.Windows.Controls.UserControl;

namespace Dynamo.Controls
{
    public delegate void SendGraphicsToViewDelegate(
        Point3DCollection points, Point3DCollection pointsSelected,
        Point3DCollection lines, Point3DCollection linesSelected, Point3DCollection redLines,
        Point3DCollection greenLines,
        Point3DCollection blueLines, Point3DCollection verts, Vector3DCollection norms,
        Int32Collection tris,
        Point3DCollection vertsSel, Vector3DCollection normsSel, Int32Collection trisSel,
        MeshGeometry3D mesh,
        MeshGeometry3D meshSel, List<BillboardTextItem> text, PointCollection textCoords);

    /// <summary>
    /// Interaction logic for WatchControl.xaml
    /// </summary>
    public partial class Watch3DView : UserControl, INotifyPropertyChanged
    {
        #region events

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion

        #region private members

        private readonly string _id="";
        private Point _rightMousePoint;
        private Point3DCollection _points = new Point3DCollection();
        private Point3DCollection _lines = new Point3DCollection();
        private Point3DCollection _xAxis = new Point3DCollection();
        private Point3DCollection _yAxis = new Point3DCollection();
        private Point3DCollection _zAxis = new Point3DCollection();
        private MeshGeometry3D _mesh = new MeshGeometry3D();
        private Point3DCollection _pointsSelected = new Point3DCollection();
        private Point3DCollection _linesSelected = new Point3DCollection();
        private MeshGeometry3D _meshSelected = new MeshGeometry3D();
        private List<Point3D> _grid = new List<Point3D>();
        private List<BillboardTextItem> _text = new List<BillboardTextItem>();
        private BitmapImage colorMap;
        private string colorMapPath;
        private Dictionary<Color,Point> colorDictionary;

        #endregion

        #region public properties

        public Material MeshMaterial
        {
            get { return Materials.White; }
        }

        public List<Point3D> Grid
        {
            get { return _grid; }
            set
            {
                _grid = value;
                NotifyPropertyChanged("Grid");
            }
        }

        public Point3DCollection Points
        {
            get { return _points; }
            set
            {
                _points = value;
            }
        }

        public Point3DCollection Lines
        {
            get { return _lines; }
            set
            {
                _lines = value;
            }
        }

        public Point3DCollection XAxes
        {
            get { return _xAxis; }
            set
            {
                _xAxis = value;
            }
        }

        public Point3DCollection YAxes
        {
            get { return _yAxis; }
            set
            {
                _yAxis = value;
            }
        }

        public Point3DCollection ZAxes
        {
            get { return _zAxis; }
            set
            {
                _zAxis = value;
            }
        }

        public MeshGeometry3D Mesh
        {
            get { return _mesh; }
            set
            {
                _mesh = value;
            }
        }

        public Point3DCollection PointsSelected
        {
            get { return _pointsSelected; }
            set
            {
                _pointsSelected = value;
            }
        }

        public Point3DCollection LinesSelected
        {
            get { return _linesSelected; }
            set
            {
                _linesSelected = value;
            }
        }

        public MeshGeometry3D MeshSelected
        {
            get { return _meshSelected; }
            set
            {
                _meshSelected = value;
            }
        }

        public List<BillboardTextItem> Text
        {
            get
            {
                return _text;
            }
            set
            {
                _text = value;
            }
        }

        public HelixViewport3D View
        {
            get { return watch_view; }
        }

        /// <summary>
        /// Used for testing to track the number of meshes that are merged
        /// during render.
        /// </summary>
        public int MeshCount { get; set; }

        public BitmapImage ColorMap
        {
            get { return colorMap; }
            set
            {
                colorMap = value;
                NotifyPropertyChanged("ColorMap");
            }
        }

        #endregion

        #region constructors

        public Watch3DView()
        {
            InitializeComponent();
            watch_view.DataContext = this;
            Loaded += OnViewLoaded;
            Dispatcher.ShutdownStarted += Dispatcher_ShutdownStarted;
        }

        public Watch3DView(string id)
        {
            InitializeComponent();
            watch_view.DataContext = this;
            Loaded += OnViewLoaded;
            Dispatcher.ShutdownStarted += Dispatcher_ShutdownStarted;

            _id = id;
        }

        #endregion

        #region event handlers

        private void Dispatcher_ShutdownStarted(object sender, EventArgs e)
        {
            Debug.WriteLine("Watch 3D view unloaded.");

            //check this for null so the designer can load the preview
            if (dynSettings.Controller != null)
            {
                dynSettings.Controller.VisualizationManager.RenderComplete -= VisualizationManagerRenderComplete;
                dynSettings.Controller.VisualizationManager.ResultsReadyToVisualize -= VisualizationManager_ResultsReadyToVisualize;
            }
        }

        private void OnViewLoaded(object sender, RoutedEventArgs e)
        {
            MouseLeftButtonDown += new MouseButtonEventHandler(view_MouseButtonIgnore);
            MouseLeftButtonUp += new MouseButtonEventHandler(view_MouseButtonIgnore);
            MouseRightButtonUp += new MouseButtonEventHandler(view_MouseRightButtonUp);
            PreviewMouseRightButtonDown += new MouseButtonEventHandler(view_PreviewMouseRightButtonDown);

            var mi = new MenuItem { Header = "Zoom to Fit" };
            mi.Click += new RoutedEventHandler(mi_Click);

            MainContextMenu.Items.Add(mi);

            //check this for null so the designer can load the preview
            if (dynSettings.Controller != null)
            {
                dynSettings.Controller.VisualizationManager.RenderComplete += VisualizationManagerRenderComplete;
                dynSettings.Controller.VisualizationManager.ResultsReadyToVisualize += VisualizationManager_ResultsReadyToVisualize;
            }

            DrawGrid();
        }

        /// <summary>
        /// Handler for the visualization manager's ResultsReadyToVisualize event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void VisualizationManager_ResultsReadyToVisualize(object sender, VisualizationEventArgs e)
        {
            //Dispatcher.Invoke(new Action<VisualizationEventArgs>(RenderDrawables), DispatcherPriority.Render,
            //                    new object[] {e});
            RenderDrawables(e);
        }

        /// <summary>
        /// When visualization is complete, the view requests it's visuals. For Full
        /// screen watch, this will be all renderables. For a Watch 3D node, this will
        /// be the subset of the renderables associated with the node.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void VisualizationManagerRenderComplete(object sender, RenderCompletionEventArgs e)
        {
            if (dynSettings.Controller == null)
            {
                return;
            }

            Dispatcher.Invoke(new Action(delegate
            {
                var vm = (IWatchViewModel) DataContext;

                if (vm.GetBranchVisualizationCommand.CanExecute(e.TaskId))
                {
                    vm.GetBranchVisualizationCommand.Execute(e.TaskId);
                }
            }));
        }

        /// <summary>
        /// Callback for thumb control's DragStarted event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ResizeThumb_OnDragStarted(object sender, DragStartedEventArgs e)
        {
            //throw new NotImplementedException();
        }

        /// <summary>
        /// Callbcak for thumb control's DragCompleted event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ResizeThumb_OnDragCompleted(object sender, DragCompletedEventArgs e)
        {
            //throw new NotImplementedException();
        }

        /// <summary>
        /// Callback for thumb control's DragDelta event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ResizeThumb_OnDragDelta(object sender, DragDeltaEventArgs e)
        {
            var yAdjust = ActualHeight + e.VerticalChange;
            var xAdjust = ActualWidth + e.HorizontalChange;

            //Debug.WriteLine("d_x:" + e.HorizontalChange + "," + "d_y:" + e.VerticalChange);
            //Debug.WriteLine("Size:" + _nodeUI.Width + "," + _nodeUI.Height);
            //Debug.WriteLine("ActualSize:" + _nodeUI.ActualWidth + "," + _nodeUI.ActualHeight);
            //Debug.WriteLine("Grid size:" + _nodeUI.ActualWidth + "," + _nodeUI.ActualHeight);

            if (xAdjust >= inputGrid.MinWidth)
            {
                Width = xAdjust;
            }

            if (yAdjust >= inputGrid.MinHeight)
            {
                Height = yAdjust;
            }
        }

        protected void mi_Click(object sender, RoutedEventArgs e)
        {
            watch_view.ZoomExtents();
        }

        private void MainContextMenu_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
        }

        void view_MouseButtonIgnore(object sender, MouseButtonEventArgs e)
        {
            e.Handled = false;
        }

        void view_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            _rightMousePoint = e.GetPosition(topControl);
        }

        void view_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            //if the mouse has moved, and this is a right click, we assume 
            // rotation. handle the event so we don't show the context menu
            // if the user wants the contextual menu they can click on the
            // node sidebar or top bar
            if (e.GetPosition(topControl) != _rightMousePoint)
            {
                e.Handled = true;
            }
        }

        private void Watch_view_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            //Point mousePos = e.GetPosition(watch_view);
            //PointHitTestParameters hitParams = new PointHitTestParameters(mousePos);
            //VisualTreeHelper.HitTest(watch_view, null, ResultCallback, hitParams);
            //e.Handled = true;
        }

        #endregion

        #region private methods

        /// <summary>
        /// Create the grid
        /// </summary>
        private void DrawGrid()
        {
            Grid = null;

            var newLines = new List<Point3D>();

            for (int x = -10; x <= 10; x++)
            {
                newLines.Add(new Point3D(x, -10, -.001));
                newLines.Add(new Point3D(x, 10, -.001));
            }

            for (int y = -10; y <= 10; y++)
            {
                newLines.Add(new Point3D(-10, y, -.001));
                newLines.Add(new Point3D(10, y, -.001));
            }

            Grid = newLines;
        }

        /// <summary>
        /// Use the render packages returned from the visualization manager to update the visuals.
        /// The visualization event arguments will contain a set of render packages and an id representing 
        /// the associated node. Visualizations for the background preview will return an empty id.
        /// </summary>
        /// <param name="e"></param>
        private void RenderDrawables(VisualizationEventArgs e)
        {
            //check the id, if the id is meant for another watch,
            //then ignore it
            if (e.Id != _id)
            {
                return;
            }

            var sw = new Stopwatch();
            sw.Start();

            // Replace this list of colors with one created from
            // the colors in the render packages. Use a linear
            // gradient of colors to test.
            colorDictionary = GenerateColorMap(CreateLinearGradient(Colors.Yellow, Colors.Purple));
            
            Points = null;
            Lines = null;
            Mesh = null;
            XAxes = null;
            YAxes = null;
            ZAxes = null;
            PointsSelected = null;
            LinesSelected = null;
            MeshSelected = null;
            Text = null;
            MeshCount = 0;

            //separate the selected packages
            var packages = e.Packages.Where(x => x.Selected == false)
                .Where(rp=>rp.TriangleVertices.Count % 9 == 0)
                .ToArray();
            var selPackages = e.Packages
                .Where(x => x.Selected)
                .Where(rp => rp.TriangleVertices.Count % 9 == 0)
                .ToArray();

            //pre-size the points collections
            var pointsCount = packages.Select(x => x.PointVertices.Count/3).Sum();
            var selPointsCount = selPackages.Select(x => x.PointVertices.Count / 3).Sum();
            var points = new Point3DCollection(pointsCount);
            var pointsSelected = new Point3DCollection(selPointsCount);

            //pre-size the lines collections
            //these sizes are conservative as the axis lines will be
            //taken from the linestripvertex collections as well.
            var lineCount = packages.Select(x => x.LineStripVertices.Count/3).Sum();
            var lineSelCount = selPackages.Select(x => x.LineStripVertices.Count / 3).Sum();
            var lines = new Point3DCollection(lineCount);
            var linesSelected = new Point3DCollection(lineSelCount);
            var redLines = new Point3DCollection(lineCount);
            var greenLines = new Point3DCollection(lineCount);
            var blueLines = new Point3DCollection(lineCount);

            //pre-size the text collection
            var textCount = e.Packages.Count(x => x.DisplayLabels);
            var text = new List<BillboardTextItem>(textCount);

            //http://blogs.msdn.com/b/timothyc/archive/2006/08/31/734308.aspx
            //presize the mesh collections
            var meshVertCount = packages.Select(x => x.TriangleVertices.Count / 3).Sum();
            var meshVertSelCount = selPackages.Select(x => x.TriangleVertices.Count / 3).Sum();

            var mesh = new MeshGeometry3D();
            var meshSel = new MeshGeometry3D();
            var verts = new Point3DCollection(meshVertCount);
            var vertsSel = new Point3DCollection(meshVertSelCount);
            var norms = new Vector3DCollection(meshVertCount);
            var normsSel = new Vector3DCollection(meshVertSelCount);
            var tris = new Int32Collection(meshVertCount);
            var trisSel = new Int32Collection(meshVertSelCount);
            var textCoords = new PointCollection();

            foreach (var package in packages)
            {
                ConvertPoints(package, points, text);
                ConvertLines(package, lines, redLines, greenLines, blueLines, text);
                ConvertMeshes(package, verts, norms, tris, textCoords);
            }

            foreach (var package in selPackages)
            {
                ConvertPoints(package, pointsSelected, text);
                ConvertLines(package, linesSelected, redLines, greenLines, blueLines, text);
                ConvertMeshes(package, vertsSel, normsSel, trisSel, textCoords);
            }

            sw.Stop();
            Debug.WriteLine(string.Format("RENDER: {0} ellapsed for updating background preview.", sw.Elapsed));

            var vm = (IWatchViewModel)DataContext;
            if (vm.CheckForLatestRenderCommand.CanExecute(e.TaskId))
            {
                vm.CheckForLatestRenderCommand.Execute(e.TaskId);
            }

            points.Freeze();
            pointsSelected.Freeze();
            lines.Freeze();
            linesSelected.Freeze();
            redLines.Freeze();
            greenLines.Freeze();
            blueLines.Freeze();
            verts.Freeze();
            norms.Freeze();
            tris.Freeze();
            vertsSel.Freeze();
            normsSel.Freeze();
            trisSel.Freeze();

            Dispatcher.Invoke(new SendGraphicsToViewDelegate(SendGraphicsToView), DispatcherPriority.Render,
                               new object[] {points, pointsSelected, lines, linesSelected, redLines, 
                                   greenLines, blueLines, verts, norms, tris, vertsSel, normsSel, 
                                   trisSel, mesh, meshSel, text, textCoords});
        }

        private void SendGraphicsToView(Point3DCollection points, Point3DCollection pointsSelected,
            Point3DCollection lines, Point3DCollection linesSelected, Point3DCollection redLines, Point3DCollection greenLines,
            Point3DCollection blueLines, Point3DCollection verts, Vector3DCollection norms, Int32Collection tris,
            Point3DCollection vertsSel, Vector3DCollection normsSel, Int32Collection trisSel, MeshGeometry3D mesh,
            MeshGeometry3D meshSel, List<BillboardTextItem> text, PointCollection textCoords)
        {
            Points = points;
            PointsSelected = pointsSelected;
            Lines = lines;
            LinesSelected = linesSelected;
            XAxes = redLines;
            YAxes = greenLines;
            ZAxes = blueLines;

            mesh.Positions = verts;
            mesh.Normals = norms;
            mesh.TriangleIndices = tris;
            mesh.TextureCoordinates = textCoords;

            meshSel.Positions = vertsSel;
            meshSel.Normals = normsSel;
            meshSel.TriangleIndices = trisSel;
            
            Mesh = mesh;
            MeshSelected = meshSel;
            Text = text;

            // Send property changed notifications for everything
            NotifyPropertyChanged(string.Empty);
        }

        private void ConvertPoints(RenderPackage p,
            ICollection<Point3D> pointColl,
            ICollection<BillboardTextItem> text)
        {
            for (int i = 0; i < p.PointVertices.Count; i += 3)
            {

                var pos = new Point3D(
                    p.PointVertices[i],
                    p.PointVertices[i + 1],
                    p.PointVertices[i + 2]);

                pointColl.Add(pos);

                if (p.DisplayLabels)
                {
                    text.Add(new BillboardTextItem {Text = CleanTag(p.Tag), Position = pos});
                }
            }
        }

        private void ConvertLines(RenderPackage p,
            ICollection<Point3D> lineColl,
            ICollection<Point3D> redLines,
            ICollection<Point3D> greenLines,
            ICollection<Point3D> blueLines,
            ICollection<BillboardTextItem> text)
        {
            int idx = 0;
            int color_idx = 0;

            int outerCount = 0;
            foreach (var count in p.LineStripVertexCounts)
            {
                for (int i = 0; i < count; ++i)
                {
                    var point = new Point3D(p.LineStripVertices[idx], p.LineStripVertices[idx + 1],
                        p.LineStripVertices[idx + 2]);

                    if (i == 0 && outerCount == 0 && p.DisplayLabels)
                    {
                        text.Add(new BillboardTextItem { Text = CleanTag(p.Tag), Position = point });
                    }

                    if (i != 0 && i != count - 1)
                    {
                        lineColl.Add(point);
                    }
                    
                    bool isAxis = false;
                    var startColor = Color.FromRgb(
                                            p.LineStripVertexColors[color_idx],
                                            p.LineStripVertexColors[color_idx + 1],
                                            p.LineStripVertexColors[color_idx + 2]);

                    if (startColor == Color.FromRgb(255, 0, 0))
                    {
                        redLines.Add(point);
                        isAxis = true;
                    }
                    else if (startColor == Color.FromRgb(0, 255, 0))
                    {
                        greenLines.Add(point);
                        isAxis = true;
                    }
                    else if (startColor == Color.FromRgb(0, 0, 255))
                    {
                        blueLines.Add(point);
                        isAxis = true;
                    }

                    if (!isAxis)
                    {
                        lineColl.Add(point);
                    } 

                    idx += 3;
                    color_idx += 4;
                }
                outerCount++;
            }
        }

        private void ConvertMeshes(RenderPackage p,
            ICollection<Point3D> points, ICollection<Vector3D> norms,
            ICollection<int> tris, ICollection<Point> textCoords)
        {
            for (int i = 0; i < p.TriangleVertices.Count; i+=3)
            {
                var new_point = new Point3D(p.TriangleVertices[i],
                                            p.TriangleVertices[i + 1],
                                            p.TriangleVertices[i + 2]);

                var normal = new Vector3D(p.TriangleNormals[i],
                                            p.TriangleNormals[i + 1],
                                            p.TriangleNormals[i + 2]);

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

                // For testing, randomly select a color from the 
                // color dictionary. Replace this with a lookup
                // of the UV with a color Key.
                var rand = new Random();
                var values = colorDictionary.Values.ToList();
                int size = colorDictionary.Count;
                textCoords.Add(values[rand.Next(size)]);
            }

            if (tris.Count > 0)
            {
                MeshCount++;
            }
        }

        private string CleanTag(string tag)
        {
            var splits = tag.Split(':');
            if (splits.Count() <= 1) return tag;

            var sb = new StringBuilder();
            for (int i = 1; i < splits.Count(); i++)
            {
                sb.AppendFormat("[{0}]", splits[i]);
            }
            return sb.ToString();
        }

        private HitTestResultBehavior ResultCallback(HitTestResult result)
        {
            // Did we hit 3D?
            var rayResult = result as RayHitTestResult;
            if (rayResult != null)
            {
                // Did we hit a MeshGeometry3D?
                var rayMeshResult =
                    rayResult as RayMeshGeometry3DHitTestResult;

                if (rayMeshResult != null)
                {
                    // Yes we did!
                    var pt = rayMeshResult.PointHit;
                    ((IWatchViewModel)DataContext).SelectVisualizationInViewCommand.Execute(new double[] { pt.X, pt.Y, pt.Z });
                    return HitTestResultBehavior.Stop;
                }
            }

            return HitTestResultBehavior.Continue;
        }

        private Dictionary<Color, Point> GenerateColorMap(IEnumerable<Color> colors)
        {
            var map = new Dictionary<Color, Point>();

            const int w = 512;
            const int h = 512;
            const int block = 8;
            var bmp = new Bitmap(w, h);
            using (var gfx = Graphics.FromImage(bmp))
            {
                // Make an image with 8x8 squares of each color
                var x = 0;
                var y = 0;
                foreach (var color in colors)
                {
                    if (map.ContainsKey(color))
                    {
                        continue;
                    }

                    using (
                        var brush =
                            new SolidBrush(
                                System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B)))
                    {
                        gfx.FillRectangle(brush, x, y, block, block);

                        // Get a UV coordinate right in the
                        // middle of the block.
                        var u = (double)((x + block/2))/(double)w;
                        var v = (double)((y + block/2))/(double)h;
                        map.Add(color, new Point(u,v));
                    }

                    x += 8;
                    if (x >= 512)
                    {
                        x = 0;
                        y += 8;
                    }
                }
            }

            using (var memory = new MemoryStream())
            {
                var bitmapImage = new BitmapImage();
                bmp.Save(memory, ImageFormat.Png);
                memory.Position = 0;
                memory.Seek(0, SeekOrigin.Begin);
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnDemand;
                bitmapImage.EndInit();
                ColorMap = bitmapImage;
            }

            return map;
        }

        private static IEnumerable<Color> CreateLinearGradient(Color minColor, Color maxColor)
        {
            var colorList = new List<Color>();
            const int size = 30;
            for (int i = 0; i < size; i++)
            {
                var avgR = minColor.R + (int)((maxColor.R - minColor.R) * i / size);
                var avgG = minColor.G + (int)((maxColor.G - minColor.G) * i / size);
                var avgB = minColor.B + (int)((maxColor.B - minColor.B) * i / size);
                colorList.Add(Color.FromArgb(255, (byte)avgR, (byte)avgG, (byte)avgB));
            }
            return colorList;
        }

        #endregion
    }
}
