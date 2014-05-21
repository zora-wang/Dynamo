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
        public IManipulator Create(NodeModel node, DynamoContext context)
        {
            return new KeyboardPointManipulator(node, context);
        }
    }

    public class KeyboardPointManipulator : IManipulator
    {
        public DynamoContext Context { get; set; }
        public NodeModel PointNode { get; set; }

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

        private void KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (!e.KeyboardDevice.IsKeyDown(Key.LeftShift)) return;

            switch (e.Key)
            {
                case Key.Up:
                    Increment(YNode);
                    break;
                case Key.Down:
                    Decrement(YNode);
                    break;
                case Key.Right:
                    Increment(XNode);
                    break;
                case Key.Left:
                    Decrement(XNode);
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
            if (node.InPorts.Count <= inputPortIndex || 
                node.InPorts[inputPortIndex].Connectors.Count == 0)
            {
                return null;
            }

            var oppositeNode = node.InPorts[inputPortIndex].Connectors[0].Start.Owner;
            return oppositeNode.Name == nodeName ? oppositeNode : null;
        }

    }


}
