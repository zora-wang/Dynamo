using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using System.Xml;
using Dynamo.Controls;
using Dynamo.Models;
using Dynamo.Utilities;
using Microsoft.FSharp.Collections;
using Value = Dynamo.FScheme.Value;

namespace Dynamo.Nodes
{
    [NodeName("Watch 3D")]
    [NodeCategory(BuiltinNodeCategories.CORE_VIEW)]
    [NodeDescription("Shows a dynamic preview of geometry.")]
    [AlsoKnownAs("Dynamo.Nodes.dyn3DPreview", "Dynamo.Nodes.3DPreview")]
    [IsDesignScriptCompatible]
    public class Watch3D : NodeWithOneOutput
    {
        //private bool _requiresRedraw;
        //private bool _isRendering;
        //private bool _canNavigateBackground = true;
        private double _watchWidth = 200;
        private double _watchHeight = 200;
        private Point3D _camPosition = new Point3D(10,10,10);
        private Vector3D _lookDirection = new Vector3D(-1,-1,-1);
        public Watch3DView View { get; private set; }

        public Watch3D()
        {
            InPortData.Add(new PortData("", "Incoming geometry objects.", typeof(object)));
            OutPortData.Add(new PortData("", "Watch contents, passed through", typeof(object)));

            RegisterAllPorts();

            ArgumentLacing = LacingStrategy.Disabled;
        }

        public override Value Evaluate(FSharpList<Value> args)
        {
            var input = args[0];
            return input;
        }

        public void SetupCustomUIElements(dynNodeView nodeUI)
        {
            var mi = new MenuItem { Header = "Zoom to Fit" };
            mi.Click += mi_Click;

            nodeUI.MainContextMenu.Items.Add(mi);

            //add a 3D viewport to the input grid
            //http://helixtoolkit.codeplex.com/wikipage?title=HelixViewport3D&referringTitle=Documentation
            //_watchView = new WatchView();
            View = new Watch3DView(GUID.ToString())
            {
                Width = _watchWidth,
                Height = _watchHeight
            };

            //override the bindings for these
            //properties for watch3d
            View.View.ShowCoordinateSystem = true;
            View.View.IsHitTestVisible = true;

            var backgroundRect = new Rectangle
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                IsHitTestVisible = false
            };
            var bc = new BrushConverter();
            var strokeBrush = (Brush)bc.ConvertFrom("#313131");
            backgroundRect.Stroke = strokeBrush;
            backgroundRect.StrokeThickness = 1;
            var backgroundBrush = new SolidColorBrush(Color.FromRgb(240, 240, 240));
            backgroundRect.Fill = backgroundBrush;

            nodeUI.grid.Children.Add(backgroundRect);
            nodeUI.grid.Children.Add(View);
            backgroundRect.SetValue(Grid.RowProperty, 2);
            backgroundRect.SetValue(Grid.ColumnSpanProperty, 3);
            View.SetValue(Grid.RowProperty, 2);
            View.SetValue(Grid.ColumnSpanProperty, 3);
            View.Margin = new Thickness(5, 0, 5, 5);
            backgroundRect.Margin = new Thickness(5, 0, 5, 5);
        }

        void mi_Click(object sender, RoutedEventArgs e)
        {
            View.View.ZoomExtents();
        }

        protected override void SaveNode(XmlDocument xmlDoc, XmlElement nodeElement, SaveContext context)
        {
            base.SaveNode(xmlDoc, nodeElement, context);
            
            var viewElement = xmlDoc.CreateElement("view");
            nodeElement.AppendChild(viewElement);
            var viewHelper = new XmlElementHelper(viewElement);

            viewHelper.SetAttribute("width", Width);
            viewHelper.SetAttribute("height", Height);

            //Bail out early if the view hasn't been created.
            if (View == null)
                return;

            var camElement = xmlDoc.CreateElement("camera");
            viewElement.AppendChild(camElement);
            var camHelper = new XmlElementHelper(camElement);

            camHelper.SetAttribute("pos_x", View.View.Camera.Position.X);
            camHelper.SetAttribute("pos_y", View.View.Camera.Position.Y);
            camHelper.SetAttribute("pos_z", View.View.Camera.Position.Z);
            camHelper.SetAttribute("look_x", View.View.Camera.LookDirection.X);
            camHelper.SetAttribute("look_y", View.View.Camera.LookDirection.Y);
            camHelper.SetAttribute("look_z", View.View.Camera.LookDirection.Z);
        }

        protected override void LoadNode(XmlNode nodeElement)
        {
            base.LoadNode(nodeElement);
            try
            {
                foreach (XmlNode node in nodeElement.ChildNodes)
                {
                    if (node.Name == "view")
                    {
                        _watchWidth = Convert.ToDouble(node.Attributes["width"].Value);
                        _watchHeight = Convert.ToDouble(node.Attributes["height"].Value);

                        foreach (XmlNode inNode in node.ChildNodes)
                        {
                            if (inNode.Name == "camera")
                            {
                                var x = Convert.ToDouble(inNode.Attributes["pos_x"].Value);
                                var y = Convert.ToDouble(inNode.Attributes["pos_y"].Value);
                                var z = Convert.ToDouble(inNode.Attributes["pos_z"].Value);
                                var lx = Convert.ToDouble(inNode.Attributes["look_x"].Value);
                                var ly = Convert.ToDouble(inNode.Attributes["look_y"].Value);
                                var lz = Convert.ToDouble(inNode.Attributes["look_z"].Value);
                                _camPosition = new Point3D(x,y,z);
                                _lookDirection = new Vector3D(lx,ly,lz);
                            }
                        }
                    }
                }
                
            }
            catch(Exception ex)
            {
                DynamoLogger.Instance.Log(ex);
                DynamoLogger.Instance.Log("View attributes could not be read from the file.");
            }
            
        }

        public override void UpdateRenderPackage()
        {
            //do nothing
            //a watch should not draw its outputs
        }
    }
}
