using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Web.UI.WebControls;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using Autodesk.DesignScript.Geometry;
using Dynamo.Controls;
using Dynamo.Models;
using Dynamo.ViewModels;
using HelixToolkit.Wpf;
using Octree.Tools;

namespace Dynamo.Manipulation
{
    public class MousePointManipulatorCreator : INodeManipulatorCreator
    {
        public IManipulator Create(NodeModel node, DynamoContext context)
        {
            return new Manipulation.MousePointManipulator(node, context);
        }
    }

    public class MousePointManipulator : IManipulator
    {
        public DynamoContext Context { get; set; }
        public NodeModel PointNode { get; set; }
        public Watch3DView Watch3DView { get; set; }
        public HelixViewport3D Helix3DView { get; set; }

        public double Velocity = 1;

        private NodeModel XNode;
        private NodeModel YNode;
        private NodeModel ZNode;

        private bool IsDragging = false;
        private int DragAxis = -1;

        private Autodesk.DesignScript.Geometry.Point Origin;
        private Autodesk.DesignScript.Geometry.Point XAxisEnd;
        private Autodesk.DesignScript.Geometry.Point YAxisEnd;
        private Autodesk.DesignScript.Geometry.Point ZAxisEnd;

        public MousePointManipulator(Models.NodeModel pointNode, DynamoContext context)
        {
            this.PointNode = pointNode;
            this.Context = context;
            this.Watch3DView = Context.View.background_preview;
            this.Helix3DView = Watch3DView.View;

            string sliderName = "Double Slider";

            XNode = pointNode.GetInputNodeOfName(0, sliderName);
            YNode = pointNode.GetInputNodeOfName(1, sliderName);
            ZNode = pointNode.GetInputNodeOfName(2, sliderName);

            Watch3DView.View.MouseMove += MouseMove;
            Watch3DView.View.MouseDown += ViewOnMouseDown;
            Watch3DView.View.MouseUp += ViewOnMouseUp;

            var val = PointNode.GetValue(0) != null ? PointNode.GetValue(0).Data : null;

            if (val is Point) DrawCS(val as Point);
        }

        private void DrawCS(Autodesk.DesignScript.Geometry.Point pt)
        {
            Origin = pt;
            XAxisEnd = pt.Add(Vector.XAxis());
            YAxisEnd = pt.Add(Vector.YAxis());
            ZAxisEnd = pt.Add(Vector.ZAxis());

            this.Watch3DView.Lines.Add(Origin.ToPoint3D());
            this.Watch3DView.Lines.Add(XAxisEnd.ToPoint3D());
            this.Watch3DView.Lines.Add(Origin.ToPoint3D());
            this.Watch3DView.Lines.Add(YAxisEnd.ToPoint3D());
            this.Watch3DView.Lines.Add(Origin.ToPoint3D());
            this.Watch3DView.Lines.Add(ZAxisEnd.ToPoint3D());


        }

        private void ViewOnMouseUp(object sender, MouseButtonEventArgs mouseButtonEventArgs)
        {
            this.IsDragging = false;
        }

        private void ViewOnMouseDown(object sender, MouseButtonEventArgs mouseButtonEventArgs)
        {
            if ( TryInitializeDrag(mouseButtonEventArgs) )
            {
                this.IsDragging = true;
            }
        }

        private bool TryInitializeDrag(MouseButtonEventArgs mouseButtonEventArgs)
        {
            var val = PointNode.GetValue(0);



            return false;
        }

        private bool DoesIntersectAxis( Point a0, Point a1, Point eye, Vector dir, double tol )
        {
            var l1 = Line.ByStartPointEndPoint(a0, a1);
            var l2 = Line.ByStartPointEndPoint(eye, eye.Add(dir.Scale(10000)));

            return l1.DistanceTo(l2) < tol;
        }

        private void MouseMove(object sender, MouseEventArgs mouseEventArgs)
        {
            Console.WriteLine("MouseMove");
            // project onto appropriate edge, set coordinate value
            //if (!this.IsDragging) return false;

        }

        private void Increment(NodeModel node)
        {
            if (node == null) return;

            dynamic uiNode = node;
            uiNode.Value = uiNode.Value + Velocity;
        }

        private void Decrement(NodeModel node)
        {
            if (node == null) return;

            dynamic uiNode = node;
            uiNode.Value = uiNode.Value - Velocity;
        }

        public void Dispose()
        {
            //Context.View.Watch3D.View.MouseMove += MouseMove;
            //Context.View.Watch3D.View.MouseDown += ViewOnMouseDown;
            //Context.View.Watch3D.View.MouseUp += ViewOnMouseUp;
        }
    }

    public static class PointExtensions
    {
        public static Point3D ToPoint3D(this Autodesk.DesignScript.Geometry.Point point)
        {
            return new Point3D(point.X, point.Y, point.Z);
        }
    }

}
