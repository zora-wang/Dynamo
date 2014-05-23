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
using System.Xml.Xsl;
using Autodesk.DesignScript.Geometry;
using Autodesk.DesignScript.Interfaces;
using Dynamo.Controls;
using Dynamo.DSEngine;
using Dynamo.Models;
using Dynamo.Nodes;
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

            Origin = Point.Origin();

            AttachHandlers();
            AssignInputNodes();
            SetAxesFromNodeModel();
            ForceRedraw();
        }

        #region Initialization

        public void AttachHandlers()
        {
            Watch3DView.View.MouseMove += MouseMove;
            Watch3DView.View.MouseDown += MouseDown;
            Watch3DView.View.MouseUp += MouseUp;
            PointNode.RenderPackageUpdate += this.DrawManipulator;
        }

        public void AssignInputNodes()
        {
            string sliderName = "Double Slider";

            int inputIndexShift = IsCartesianPoint() ? 1 : 0;
            XNode = PointNode.GetInputNodeOfName(inputIndexShift, sliderName);
            YNode = PointNode.GetInputNodeOfName(1 + inputIndexShift, sliderName);
            ZNode = PointNode.GetInputNodeOfName(2 + inputIndexShift, sliderName);
        }

        public bool IsCartesianPoint()
        {
            return PointNode is DSFunction &&
                   (PointNode as DSFunction).Definition.MangledName ==
                  "Autodesk.DesignScript.Geometry.Point.ByCartesianCoordinates@Autodesk.DesignScript.Geometry.CoordinateSystem,double,double,double";
        }

        public void SetAxesFromNodeModel()
        {
            if (IsCartesianPoint())
            {
                var csNode = PointNode.GetInputNode(0);

                if (csNode != null)
                {
                    var cs = csNode.GetCachedValueOrDefault<CoordinateSystem>();
                    if (cs != null)
                    {
                        XAxis = cs.XAxis;
                        YAxis = cs.YAxis;
                        ZAxis = cs.ZAxis;
                        return;
                    }
                }
            }

            XAxis = Vector.XAxis();
            YAxis = Vector.YAxis();
            ZAxis = Vector.ZAxis();

        }

        #endregion

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
                    pkgs.Add(BuildConstrainedAxisLine(XAxis));
                }
            }

            if (YNode != null)
            {
                pkgs.Add(BuildAxisHandle(YAxis, System.Drawing.Color.Lime));

                if (DragY)
                {
                    pkgs.Add(BuildConstrainedAxisLine(YAxis));
                }
            }

            if (ZNode != null)
            {
                pkgs.Add(BuildAxisHandle(ZAxis, System.Drawing.Color.Blue));

                if (DragZ)
                {
                    pkgs.Add(BuildConstrainedAxisLine(ZAxis));
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
                axis.Direction = XAxis;
            }
            else if (DragY)
            {
                axis.Direction = YAxis;
            }
            else if (DragZ)
            {
                axis.Direction = ZAxis;
            }

            var moveVector = GetMoveVector(axis, GetClickRay(mouseEventArgs));

            // this is where we expected the manipulator to be when drag resumes
            ExpectedPosition = Origin.Add(moveVector);

            if (DragX)
            {
                Move(XNode, moveVector.Dot(XAxis));
            }
            else if (DragY)
            {
                Move(YNode, moveVector.Dot(YAxis));
            }
            else if (DragZ)
            {
                Move(ZNode, moveVector.Dot(ZAxis));
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

        private Ray GetClickRay(MouseEventArgs mouseButtonEventArgs)
        {
            var mousePos = mouseButtonEventArgs.GetPosition(Watch3DView);

            var width = this.Watch3DView.ActualWidth;
            var height = this.Watch3DView.ActualHeight;

            var c = Helix3DView.Camera as PerspectiveCamera;

            return Ray.FromMouseClick(c, width, height, mousePos.X, mousePos.Y);
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

    public struct Ray
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

        public static Ray FromMouseClick(PerspectiveCamera c, double width, double height, double mouseX, double mouseY)
        {
            var fov = c.FieldOfView * Math.PI / 180;

            var y = c.UpDirection.ToVector();
            var z = c.LookDirection.ToVector().Normalized();
            var x = z.Cross(y).Normalized();
            y = z.Cross(x).Normalized();

            // determine coordinates of the point click on the view plane
            var ncx = (mouseX - width * 0.5) * (1.0 / width) * 2;
            var ncy = (mouseY - height * 0.5) * (1.0 / width) * 2;

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
    }

    public static class NodeExtensions
    {
        public static T GetCachedValueOrDefault<T>(this NodeModel node) where T : class
        {
            object val;
            try
            {
                val = node.CachedValue != null ? node.CachedValue.Data : null;
            }
            catch
            {
                val = null;
            }

            return val is T ? val as T : null;
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
