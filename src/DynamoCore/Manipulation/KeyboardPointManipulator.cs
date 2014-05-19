using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using Dynamo.Models;

namespace Dynamo.Manipulation
{
    public class KeyboardPointManipulatorCreator : INodeManipulatorCreator
    {

        public string NodeType
        {
            get
            {
                return "Point.ByCoordinates@double,double,double";
            }
        }

        public IManipulator Create(NodeModel node, DynamoContext context)
        {
            return new KeyboardPointManipulator(node, context);
        }

    }

    public class KeyboardPointManipulator : IManipulator
    {
        public DynamoContext Context { get; set; }
        public NodeModel PointNode { get; set; }
        public Tuple<bool, bool, bool> FreeAxes { private get; set; }

        public double Velocity = 1;

        private NodeModel XNode;
        private NodeModel YNode;
        private NodeModel ZNode;

        public KeyboardPointManipulator(Models.NodeModel pointNode, DynamoContext context)
        {
            this.PointNode = pointNode;
            this.Context = context;

            Context.View.KeyUp += this.KeyUp;

            string sliderName = "Double Slider";

            XNode = pointNode.GetInputNodeOfName( 0, sliderName );
            YNode = pointNode.GetInputNodeOfName( 1, sliderName );
            ZNode = pointNode.GetInputNodeOfName( 2, sliderName );         

        }

        private void IncrementValue(NodeModel node)
        {
            if (node == null) return;

            dynamic uiNode = node;
            uiNode.Value = uiNode.Value + Velocity;
        }

        private void DecrementValue(NodeModel node)
        {
            if (node == null) return;

            dynamic uiNode = node;
            uiNode.Value = uiNode.Value - Velocity;
        }

        private void KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Up:
                    IncrementValue(YNode);
                    break;
                case Key.Down:
                    DecrementValue(YNode);
                    break;
                case Key.Left:
                    IncrementValue(XNode);
                    break;
                case Key.Right:
                    DecrementValue(XNode);
                    break;
                case Key.X:
                    DecrementValue(ZNode);
                    break;
                case Key.Z:
                    DecrementValue(ZNode);
                    break;
            }
        }

        public void Dispose()
        {
            Context.View.KeyUp -= this.KeyUp;
        }
    }

    public static class ManipulatorExtensions
    {
        public static NodeModel GetInputNodeOfName(this NodeModel node, int inputPortIndex, string nodeName)
        {
            if (node.Name != nodeName || 
                node.InPorts.Count <= inputPortIndex || 
                node.InPorts[inputPortIndex].Connectors.Count == 0)
            {
                return null;
            }

            var oppositeNode = node.InPorts[inputPortIndex].Connectors[0].Start.Owner;
            return oppositeNode.Name != nodeName ? oppositeNode : null;
        }

    }


}
