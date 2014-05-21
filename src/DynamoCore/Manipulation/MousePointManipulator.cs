using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Web.UI.WebControls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Autodesk.DesignScript.Geometry;
using Autodesk.DesignScript.Interfaces;
using Dynamo.Controls;
using Dynamo.DSEngine;
using Dynamo.Models;
using Dynamo.Utilities;
using Dynamo.ViewModels;
using HelixToolkit.Wpf;
using Octree.Tools;
using Point = Autodesk.DesignScript.Geometry.Point;

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

        private bool DragX = false;
        private bool DragY = false;
        private bool DragZ = false;

        private Autodesk.DesignScript.Geometry.Point Origin;

        private Autodesk.DesignScript.Geometry.Point XAxisEnd;
        private Autodesk.DesignScript.Geometry.Point YAxisEnd;
        private Autodesk.DesignScript.Geometry.Point ZAxisEnd;

        private Point ExpectedPosition;

        public MousePointManipulator(Models.NodeModel pointNode, DynamoContext context)
        {
            this.PointNode = pointNode;
            this.Context = context;
            this.Watch3DView = Context.View.background_preview;
            this.Helix3DView = Watch3DView.View;

            string sliderName = "Double Slider";

            Origin = Point.Origin();

            XNode = pointNode.GetInputNodeOfName(0, sliderName);
            YNode = pointNode.GetInputNodeOfName(1, sliderName);
            ZNode = pointNode.GetInputNodeOfName(2, sliderName);

            Watch3DView.View.MouseMove += MouseMove;
            Watch3DView.View.MouseDown += ViewOnMouseDown;
            Watch3DView.View.MouseUp += ViewOnMouseUp;
            PointNode.RenderPackageUpdate += this.DrawManipulator;
            
            // hack to add the manipulator
            ForceReevaluation();
        }

        private void ForceReevaluation()
        {
            if (XNode != null)
            {
                Reset(XNode);
            }
            else if (YNode != null)
            {
                Reset(YNode);
            }
            else if (YNode != null)
            {
                Reset(ZNode);
            }
        }

        private void UpdatePosition()
        {
            if (PointNode == null) return;

            // hack to prevent this from throwing an exception
            object val;
            try
            {
                val = PointNode.GetValue(0) != null ? PointNode.GetValue(0).Data : null;
            }
            catch
            {
                val = null;
            }
           
            Origin = val as Point ?? this.Origin ?? Point.Origin();
        }

        private void UpdateAxes()
        {
            XAxisEnd = Origin.Add(Vector.XAxis());
            YAxisEnd = Origin.Add(Vector.YAxis());
            ZAxisEnd = Origin.Add(Vector.ZAxis());
        }

        private void DrawManipulator(NodeModel node)
        {
            if (!PointNode.IsSelected) return;

            Console.WriteLine("DRAW");
            this.UpdatePosition();
            this.UpdateAxes();

            PointNode.RenderPackages.AddRange(BuildRenderPackages());
        }

        private IEnumerable<RenderPackage> BuildRenderPackages()
        {
            var pkgX = new RenderPackage();
            var pkgY = new RenderPackage();
            var pkgZ = new RenderPackage();
            var pkgs = new List<RenderPackage>();

            if (XNode != null)
            {
                pkgX.PushLineStripVertexCount(2);
                pkgX.PushLineStripVertexColor(255, 0, 0, 255);
                pkgX.PushLineStripVertex(Origin.X, Origin.Y, Origin.Z);
                pkgX.PushLineStripVertexColor(255, 0, 0, 255);
                pkgX.PushLineStripVertex(XAxisEnd.X, XAxisEnd.Y, XAxisEnd.Z);
                pkgs.Add(pkgX);
            }

            if (YNode != null)
            {
                pkgY.PushLineStripVertexCount(2);
                pkgY.PushLineStripVertexColor(0, 255, 0, 255);
                pkgY.PushLineStripVertex(Origin.X, Origin.Y, Origin.Z);
                pkgY.PushLineStripVertexColor(0, 255, 0, 255);
                pkgY.PushLineStripVertex(YAxisEnd.X, YAxisEnd.Y, YAxisEnd.Z);
                pkgs.Add(pkgY);
            }

            if (ZNode != null)
            {
                pkgZ.PushLineStripVertexCount(2);
                pkgZ.PushLineStripVertexColor(0, 0, 255, 255);
                pkgZ.PushLineStripVertex(Origin.X, Origin.Y, Origin.Z);
                pkgZ.PushLineStripVertexColor(0, 0, 255, 255);
                pkgZ.PushLineStripVertex(ZAxisEnd.X, ZAxisEnd.Y, ZAxisEnd.Z);
                pkgs.Add(pkgZ);
            }

            return pkgs;
        }

        private void ViewOnMouseUp(object sender, MouseButtonEventArgs mouseButtonEventArgs)
        {
            ResetDrag();
        }

        private void ViewOnMouseDown(object sender, MouseButtonEventArgs mouseButtonEventArgs)
        {
            UpdatePosition();
            UpdateAxes();
            ResetDrag();

            var ray = GetClickRay(mouseButtonEventArgs);

            var dragX = DoesIntersectAxis(Origin, XAxisEnd, ray, 0.1);
            var dragY = DoesIntersectAxis(Origin, YAxisEnd, ray, 0.1);
            var dragZ = DoesIntersectAxis(Origin, ZAxisEnd, ray, 0.1);

            // for now, arbitrarily take the first successful pick axis
            if (dragX)
            {
                DragX = true;
            }
            else if (dragY)
            {
                DragY = true;
            }
            else if (dragZ)
            {
                DragZ = true;
            }

            ExpectedPosition = Origin;
        }

        struct Ray
        {
            public Point Origin;
            public Vector Direction;

            public Line ToLine()
            {
                return Line.ByStartPointEndPoint(Origin, Origin.Add(Direction.Scale(10000)));
            }

            public Line ToOriginCenteredLine()
            {
                return Line.ByStartPointEndPoint( Origin.Add(Direction.Scale(-100)), 
                    Origin.Add(Direction.Scale(100)));
            }
        }

        private Ray GetClickRay(MouseEventArgs mouseButtonEventArgs)
        {
            var mousePos = mouseButtonEventArgs.GetPosition(Watch3DView);

            var width = this.Watch3DView.ActualWidth;
            var height = this.Watch3DView.ActualHeight;

            var c = Helix3DView.Camera as PerspectiveCamera;

            var fov = c.FieldOfView*Math.PI/180;

            var y = c.UpDirection.ToVector();
            var z = c.LookDirection.ToVector().Normalized();
            var x = z.Cross(y).Normalized();
            y = z.Cross(x).Normalized();

            // determine coordinates of the point click on the view plane
            var ncx = (mousePos.X - width*0.5)*(1.0/width)*2;
            var ncy = (mousePos.Y - height * 0.5) * (1.0 / width)*2;

            var dist = 1 / System.Math.Tan(fov / 2);

            var dx = x.Scale(ncx);
            var dy = y.Scale(ncy);

            var rayDir = z.Scale(dist).Add(dx.Add(dy)).Normalized();
            var rayOrigin = c.Position.ToPoint();

            return new Ray()
            {
                Direction = rayDir,
                Origin = rayOrigin
            };
        }

        public bool IsDragging()
        {
            return DragX || DragY || DragZ;
        }

        private void ResetDrag()
        {
            DragX = false;
            DragY = false;
            DragZ = false;
        }

        private bool DoesIntersectAxis( Point a0, Point a1, Ray ray, double tol )
        {
            var l1 = Line.ByStartPointEndPoint(a0, a1);
            var l2 = ray.ToLine();

            return l1.DistanceTo(l2) < tol;
        }

        private void MouseMove(object sender, MouseEventArgs mouseEventArgs)
        {
            if (!IsDragging()) return;

            UpdatePosition();

            if (!ExpectedPosition.IsAlmostEqualTo(Origin))
            {
                // drag call is likely not yet executed, wait until it is 
                return;  
            }

            var axis = new Ray {Origin = Origin};

            if (DragX)
            {
                axis.Direction = Vector.XAxis();
            }
            else if (DragY)
            {
                axis.Direction = Vector.YAxis(); 
            }
            else if (DragZ)
            {
                axis.Direction = Vector.ZAxis();
            }

            var moveVector = GetMoveVector(axis, GetClickRay(mouseEventArgs));

            // this is where we expected the manipulator to be when drag resumes
            ExpectedPosition = Origin.Add(moveVector);

            if (DragX)
            {
                Move(XNode, moveVector.X);
            }
            else if (DragY)
            {
                Move(YNode, moveVector.Y);
            }
            else if (DragZ)
            {
                Move(ZNode, moveVector.Z);
            }
        }

        private Vector GetMoveVector(Ray axis, Ray mouseClickRay)
        {
            var axisLine = axis.ToOriginCenteredLine();
            var mouseLine = mouseClickRay.ToLine();

            return axisLine.GetClosestPoint(mouseLine).AsVector()
                .Subtract(Origin.AsVector());
        }

        private void Move(NodeModel node, double amount)
        {
            if (node == null) return;

            if (Math.Abs(amount) < 0.001) return;

            Console.WriteLine(amount);

            dynamic uiNode = node;
            uiNode.Value = uiNode.Value + amount;
        }

        private void Reset(NodeModel node)
        {
            if (node == null) return;

            dynamic uiNode = node;
            uiNode.Value = uiNode.Value + 0.00001;
        }

        public void Dispose()
        {
            PointNode.RenderPackageUpdate -= this.DrawManipulator;
            Watch3DView.View.MouseMove -= MouseMove;
            Watch3DView.View.MouseDown -= ViewOnMouseDown;
            Watch3DView.View.MouseUp -= ViewOnMouseUp;

            PointNode.RenderPackages = new List<IRenderPackage>(){
                PointNode.RenderPackages.First()
            };

            Console.WriteLine("DISPOSE!");
            // hack to remove the coordinate system
            ForceReevaluation();
        }
    }

    public static class PointExtensions
    {
        public static Point3D ToPoint3D(this Autodesk.DesignScript.Geometry.Point point)
        {
            return new Point3D(point.X, point.Y, point.Z);
        }

        public static Point ToPoint(this Point3D point)
        {
            return Point.ByCoordinates(point.X, point.Y, point.Z);
        }

        public static Vector ToVector(this Vector3D vec)
        {
            return Vector.ByCoordinates(vec.X, vec.Y, vec.Z);
        }
    }

}
