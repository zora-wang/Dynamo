//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Windows.Input;
//using Dynamo.Models;

//namespace Dynamo.Manipulation
//{
//    public class MousePointManipulatorCreator : INodeManipulatorCreator
//    {
//        public IManipulator Create(NodeModel node, DynamoContext context)
//        {
//            return new Manipulation.MousePointManipulator(node, context);
//        }
//    }

//    public class MousePointManipulator : IManipulator
//    {
//        public DynamoContext Context { get; set; }
//        public NodeModel PointNode { get; set; }

//        public double Velocity = 1;

//        private NodeModel XNode;
//        private NodeModel YNode;
//        private NodeModel ZNode;

//        public MousePointManipulator(Models.NodeModel pointNode, DynamoContext context)
//        {
//            this.PointNode = pointNode;
//            this.Context = context;

//            Context.View.KeyUp += this.KeyUp;

//            string sliderName = "Double Slider";

//            XNode = pointNode.GetInputNodeOfName(0, sliderName);
//            YNode = pointNode.GetInputNodeOfName(1, sliderName);
//            ZNode = pointNode.GetInputNodeOfName(2, sliderName);

            
//        }

//        private void ClickInViewport()
//        {
            
//        }


//        private void Increment(NodeModel node)
//        {
//            if (node == null) return;

//            dynamic uiNode = node;
//            uiNode.Value = uiNode.Value + Velocity;
//        }

//        private void Decrement(NodeModel node)
//        {
//            if (node == null) return;

//            dynamic uiNode = node;
//            uiNode.Value = uiNode.Value - Velocity;
//        }

//        public void Dispose()
//        {
//            Context.View.KeyUp -= this.KeyUp;
//        }
//    }
//}
