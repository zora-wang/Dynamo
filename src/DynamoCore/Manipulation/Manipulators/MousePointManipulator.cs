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
using Point = Autodesk.DesignScript.Geometry.Point;

namespace Dynamo.Manipulation
{
    public class MousePointManipulatorCreator : INodeManipulatorCreator
    {
        public IManipulator Create(NodeModel node, DynamoManipulatorContext manipulatorContext)
        {
            return new Manipulation.MousePointManipulator(node, manipulatorContext);
        }
    }

    public class MousePointManipulator : IManipulator
    {
        #region Properties/Fields

        public DynamoManipulatorContext ManipulatorContext { get; set; }
        public NodeModel PointNode { get; set; }
        public Watch3DView Watch3DView { get; set; }
        public HelixViewport3D Helix3DView { get; set; }

        public bool Active { get; set; }

        private NodeModel XNode;
        private NodeModel YNode;
        private NodeModel ZNode;

        private bool DragX = false;
        private bool DragY = false;
        private bool DragZ = false;

        private Autodesk.DesignScript.Geometry.Point Origin;

        private Autodesk.DesignScript.Geometry.Vector XAxis;
        private Autodesk.DesignScript.Geometry.Vector YAxis;
        private Autodesk.DesignScript.Geometry.Vector ZAxis;

        private Point ExpectedPosition;

        #endregion

        public MousePointManipulator(Models.NodeModel pointNode, DynamoManipulatorContext manipulatorContext)
        {
            this.PointNode = pointNode;
            this.ManipulatorContext = manipulatorContext;
            this.Watch3DView = ManipulatorContext.View.background_preview;
            this.Helix3DView = Watch3DView.View;

            string sliderName = "Double Slider";

            Origin = Point.Origin();

            XNode = pointNode.GetInputNodeOfName(0, sliderName);
            YNode = pointNode.GetInputNodeOfName(1, sliderName);
            ZNode = pointNode.GetInputNodeOfName(2, sliderName);

            XAxis = Vector.XAxis();
            YAxis = Vector.YAxis();
            ZAxis = Vector.ZAxis();

            Watch3DView.View.MouseMove += MouseMove;
            Watch3DView.View.MouseDown += MouseDown;
            Watch3DView.View.MouseUp += MouseUp;
            PointNode.RenderPackageUpdate += this.DrawManipulator;
            
            ForceRedraw();
        }

        #region Drawing

        private void ForceRedraw()
        {
            // Note:  This is a hack. Should be able to stimulate redraw without recompute
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

        private void DrawManipulator(NodeModel node)
        {
            if (!PointNode.IsSelected) return;

            this.UpdatePosition();

            if (!this.Active || this.Origin == null) return;

            PointNode.RenderPackages.AddRange(BuildRenderPackages());
        }

        private IEnumerable<RenderPackage> BuildRenderPackages()
        {
            var pkgs = new List<RenderPackage>();

            if (XNode != null)
            {
                pkgs.Add(BuildAxisHandle(XAxis, System.Drawing.Color.Red));

                if (DragX)
                {
                    pkgs.Add(BuildConstrainedAxisLine(Vector.XAxis()));
                }
            }

            if (YNode != null)
            {
                pkgs.Add(BuildAxisHandle(YAxis, System.Drawing.Color.Lime));

                if (DragY)
                {
                    pkgs.Add(BuildConstrainedAxisLine(Vector.YAxis()));
                }
            }

            if (ZNode != null)
            {
                pkgs.Add(BuildAxisHandle(ZAxis, System.Drawing.Color.Blue));

                if (DragZ)
                {
                    pkgs.Add(BuildConstrainedAxisLine(Vector.ZAxis()));
                }

            }

            return pkgs;
        }

        private RenderPackage BuildAxisHandle(Vector axis, System.Drawing.Color color)
        {
            var axisEnd = this.Origin.Add(axis);

            var pkgX = new RenderPackage();

            pkgX.PushLineStripVertexCount(2);
            pkgX.PushLineStripVertexColor(color.R, color.G, color.B, color.A);
            pkgX.PushLineStripVertex(Origin.X, Origin.Y, Origin.Z);
            pkgX.PushLineStripVertexColor(color.R, color.G, color.B, color.A);
            pkgX.PushLineStripVertex(axisEnd.X, axisEnd.Y, axisEnd.Z);

            return pkgX;
        }

        private RenderPackage BuildConstrainedAxisLine(Vector axis)
        {
            var ray = new Ray()
            {
                Origin = this.Origin,
                Direction = axis
            };

            var xl = ray.ToOriginCenteredLine();

            var pkgXL = new RenderPackage();
            pkgXL.PushLineStripVertexCount(2);
            pkgXL.PushLineStripVertexColor(100, 100, 100, 255);
            pkgXL.PushLineStripVertex(xl.StartPoint.X, xl.StartPoint.Y, xl.StartPoint.Z);
            pkgXL.PushLineStripVertexColor(100, 100, 100, 255);
            pkgXL.PushLineStripVertex(xl.EndPoint.X, xl.EndPoint.Y, xl.EndPoint.Z);

            return pkgXL;
        }

        #endregion

        #region Mouse handlers

        private void MouseMove(object sender, MouseEventArgs mouseEventArgs)
        {
            if (!IsDragging()) return;

            UpdatePosition();

            if (!ExpectedPosition.IsAlmostEqualTo(Origin))
            {
                // drag call is likely not yet executed, wait until it is 
                return;
            }

            var axis = new Ray { Origin = Origin };

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

        private void MouseUp(object sender, MouseButtonEventArgs mouseButtonEventArgs)
        {
            ResetDrag();
            ForceRedraw(); // this cleans up all of the geometry
        }

        private void MouseDown(object sender, MouseButtonEventArgs mouseButtonEventArgs)
        {
            UpdatePosition();
            ResetDrag();

            if (!Active || Origin == null) return;

            var ray = GetClickRay(mouseButtonEventArgs);

            var dragX = DoesRayIntersectLine(Origin, Origin.Add(XAxis), ray, 0.15);
            var dragY = DoesRayIntersectLine(Origin, Origin.Add(YAxis), ray, 0.15);
            var dragZ = DoesRayIntersectLine(Origin, Origin.Add(ZAxis), ray, 0.15);

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

        #endregion

        #region State update

        private void UpdatePosition()
        {
            if (PointNode == null) return;

            // hack: to prevent node mirror value lookup from throwing an exception
            object val;
            try
            {

                val = PointNode.CachedValue != null ? PointNode.CachedValue.Data : null;
            }
            catch
            {
                val = null;
            }

            Origin = val as Point ?? this.Origin;

            if (Origin == null)
            {
                this.Active = false;
                Origin = Point.Origin();
                return;
            }

            this.Active = true;
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

        #endregion

        #region Geometric helpers

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
                return Line.ByStartPointEndPoint(Origin.Add(Direction.Scale(-100)),
                    Origin.Add(Direction.Scale(100)));
            }
        }

        private Ray GetClickRay(MouseEventArgs mouseButtonEventArgs)
        {
            var mousePos = mouseButtonEventArgs.GetPosition(Watch3DView);

            var width = this.Watch3DView.ActualWidth;
            var height = this.Watch3DView.ActualHeight;

            var c = Helix3DView.Camera as PerspectiveCamera;

            var fov = c.FieldOfView * Math.PI / 180;

            var y = c.UpDirection.ToVector();
            var z = c.LookDirection.ToVector().Normalized();
            var x = z.Cross(y).Normalized();
            y = z.Cross(x).Normalized();

            // determine coordinates of the point click on the view plane
            var ncx = (mousePos.X - width * 0.5) * (1.0 / width) * 2;
            var ncy = (mousePos.Y - height * 0.5) * (1.0 / width) * 2;

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

        private bool DoesRayIntersectLine( Point a0, Point a1, Ray ray, double tol )
        {
            var l1 = Line.ByStartPointEndPoint(a0, a1);
            var l2 = ray.ToLine();

            return l1.DistanceTo(l2) < tol;
        }

        private Vector GetMoveVector(Ray axis, Ray mouseClickRay)
        {
            var axisLine = axis.ToOriginCenteredLine();
            var mouseLine = mouseClickRay.ToLine();

            return axisLine.GetClosestPoint(mouseLine).AsVector()
                .Subtract(Origin.AsVector());
        }

        #endregion

        #region Node value update

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

            // hack: to force node update
            dynamic uiNode = node;
            uiNode.Value = uiNode.Value + 0.00001;
        }

        #endregion

        public void Dispose()
        {
            PointNode.RenderPackageUpdate -= this.DrawManipulator;
            Helix3DView.MouseMove -= MouseMove;
            Helix3DView.MouseDown -= MouseDown;
            Helix3DView.MouseUp -= MouseUp;

            // hack to remove the coordinate system
            if (PointNode.RenderPackages != null && PointNode.RenderPackages.Any())
            {
                PointNode.RenderPackages = new List<IRenderPackage>(){
                    PointNode.RenderPackages.First()
                };
            }

            ForceRedraw();
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
