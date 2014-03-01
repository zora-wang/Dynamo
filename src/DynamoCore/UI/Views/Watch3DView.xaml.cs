using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Dynamo.Utilities;
using Dynamo.ViewModels;
using HelixToolkit.Wpf.SharpDX;
using HitTestResult = System.Windows.Media.HitTestResult;
using Point = System.Windows.Point;

namespace Dynamo.Controls
{
    /// <summary>
    /// Interaction logic for WatchControl.xaml
    /// </summary>
    public partial class Watch3DView : UserControl
    {
        Point _rightMousePoint;
        private Watch3DViewModel _vm;
        private bool _requiresUpdate = false;

        public Viewport3DX View
        {
            get { return watch_view; }
        }

        public Watch3DView()
        {
            InitializeComponent();
            _vm = new Watch3DViewModel("", false);
            DataContext = _vm;
            Loaded += WatchViewFullscreen_Loaded;
        }

        public Watch3DView(string id)
        {
            InitializeComponent();
            _vm = new Watch3DViewModel(id, true);
            DataContext = _vm;
            Loaded += WatchViewFullscreen_Loaded;
        }

        void WatchViewFullscreen_Loaded(object sender, RoutedEventArgs e)
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
                dynSettings.Controller.VisualizationManager.VisualizationUpdateComplete += VisualizationManager_VisualizationUpdateComplete;
                dynSettings.Controller.VisualizationManager.ResultsReadyToVisualize += VisualizationManager_ResultsReadyToVisualize;
            }
        }

        /// <summary>
        /// Handler for the visualization manager's ResultsReadyToVisualize event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void VisualizationManager_ResultsReadyToVisualize(object sender, VisualizationEventArgs e)
        {
            //Dispatcher.Invoke(new Action<VisualizationEventArgs>(_vm.RenderDrawables), DispatcherPriority.Render,
                                //new object[] { e });

            _vm.RenderDrawables(e);

            models.Children.Clear();

            if (_vm.HelixMesh != null && _vm.HelixMesh.Positions.Any())
            {
                var mesh = new MeshGeometryModel3D
                {
                    Geometry = _vm.HelixMesh,
                    Material = PhongMaterials.Blue,
                    Transform = Transform3D.Identity
                };
                models.Children.Add(mesh);
            }

            if (_vm.HelixMeshSelected != null && _vm.HelixMeshSelected.Positions.Any())
            {
                var mesh = new MeshGeometryModel3D
                {
                    Geometry = _vm.HelixMeshSelected,
                    Material = PhongMaterials.Red,
                    Transform = Transform3D.Identity
                };
                models.Children.Add(mesh);
            }

            if (_vm.HelixLines != null && _vm.HelixLines.Positions.Any())
            {
                var lines = new LineGeometryModel3D
                {
                    Geometry = _vm.HelixLines,
                    Color = SharpDX.Color.Blue,
                    Transform = Transform3D.Identity,
                    Thickness = 1
                };
                models.Children.Add(lines);
            }

            if (_vm.HelixLinesSelected != null && _vm.HelixLinesSelected.Positions.Any())
            {
                var lines = new LineGeometryModel3D
                {
                    Geometry = _vm.HelixLinesSelected,
                    Color = SharpDX.Color.Cyan,
                    Transform = Transform3D.Identity,
                    Thickness = 2
                };
                models.Children.Add(lines);
            }

            if (_vm.HelixXAxes != null && _vm.HelixXAxes.Positions.Any())
            {
                var lines = new LineGeometryModel3D
                {
                    Geometry = _vm.HelixXAxes,
                    Color = SharpDX.Color.Red,
                    Transform = Transform3D.Identity,
                    Thickness = 2
                };
                models.Children.Add(lines);
            }

            if (_vm.HelixYAxes != null && _vm.HelixYAxes.Positions.Any())
            {
                var lines = new LineGeometryModel3D
                {
                    Geometry = _vm.HelixYAxes,
                    Color = SharpDX.Color.Green,
                    Transform = Transform3D.Identity,
                    Thickness = 2
                };
                models.Children.Add(lines);
            }

            if (_vm.HelixZAxes != null && _vm.HelixZAxes.Positions.Any())
            {
                var lines = new LineGeometryModel3D
                {
                    Geometry = _vm.HelixZAxes,
                    Color = SharpDX.Color.Blue,
                    Transform = Transform3D.Identity,
                    Thickness = 2
                };
                models.Children.Add(lines);
            }

            watch_view.ReAttach();
        }

        /// <summary>
        /// When visualization is complete, the view requests it's visuals. For Full
        /// screen watch, this will be all renderables. For a Watch 3D node, this will
        /// be the subset of the renderables associated with the node.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void VisualizationManager_VisualizationUpdateComplete(object sender, EventArgs e)
        {
            if (dynSettings.Controller == null)
                return;

            Dispatcher.Invoke(new Action(delegate
            {
                var vm = (IWatchViewModel) DataContext;
                   
                if (vm.GetBranchVisualizationCommand.CanExecute(null))
                {
                    vm.GetBranchVisualizationCommand.Execute(null);
                }
            }));
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
            Point mousePos = e.GetPosition(watch_view);
            PointHitTestParameters hitParams = new PointHitTestParameters(mousePos);
            VisualTreeHelper.HitTest(watch_view, null, ResultCallback, hitParams);
            e.Handled = true;
        }

        public HitTestResultBehavior ResultCallback(HitTestResult result)
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
    }
}
