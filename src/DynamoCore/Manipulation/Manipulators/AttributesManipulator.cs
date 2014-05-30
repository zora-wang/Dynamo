using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using Dynamo.Models;
using Dynamo.Nodes;
using Dynamo.ViewModels;
using Microsoft.Practices.Prism.Logging;
using Attribute = Dynamo.ViewModels.Attribute;

namespace Dynamo.Manipulation.Manipulators
{
    public class AttributesManipulatorCreator : INodeManipulatorCreator
    {
        public IManipulator Create(NodeModel node, DynamoManipulatorContext manipulatorContext)
        {
            return new AttributesManipulator(node, manipulatorContext.AttributesViewModel);
        }
    }

    public class AttributesManipulator : IManipulator
    {
        private readonly ObservableCollection<AttributeCategory> attributes;
        private readonly AttributeCategory category;

        public AttributesManipulator(NodeModel node, AttributesViewModel attributesViewModel)
        {
            attributes = attributesViewModel.Categories;
            category = GetNodeAttributeCategory(node);
            attributes.Add(category);
        }

        private IEnumerable<IAttribute> GetNodeAttributes(NodeModel node)
        {
            foreach (var i in Enumerable.Range(0, node.InPortData.Count))
            {
                Tuple<int, NodeModel> input;
                if (!node.TryGetInput(i, out input)) 
                    continue;
                var inputNode = input.Item2;
                Attribute a;
                if (AttributeLookup.TryGetAttribute(node.InPortData[i].NickName, inputNode, out a))
                    yield return a;
            }
        }

        private AttributeCategory GetNodeAttributeCategory(NodeModel node)
        {
            return new AttributeCategory(node.NickName, GetNodeAttributes(node));
        }

        public void Dispose()
        {
            attributes.Remove(category);
        }
    }

    public static class AttributeLookup
    {
        public static bool TryGetAttribute(string name, NodeModel inputNode, out Attribute attr)
        {
            AttributeUICreator e;
            if (creators.TryGetValue(inputNode.GetType().FullName, out e))
            {
                attr = new Attribute(name, e.BuildUI(inputNode));
                return true;
            }
            attr = null;
            return false;
        }

        private static readonly Dictionary<string, AttributeUICreator> creators = 
            new Dictionary<string, AttributeUICreator>
            {
                { "Dynamo.Nodes.DoubleSlider", SliderUICreator<double>.Instance },
                { "Dynamo.Nodes.IntegerSlider", SliderUICreator<int>.Instance }
            };
    }

    public interface AttributeUICreator
    {
        UIElement BuildUI(NodeModel node);
    }

    public class SliderUICreator<T> : AttributeUICreator
    {
        private static SliderUICreator<T> instance;
        public static SliderUICreator<T> Instance
        {
            get
            {
                return instance ?? (instance = new SliderUICreator<T>());
            }
        }

        private SliderUICreator() { }

        public UIElement BuildUI(NodeModel node)
        {
            var sliderNode = node as dynamic;

            var slider = new DynamoSlider(node)
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                MinWidth = 150,
                TickPlacement = TickPlacement.None,
                Value = sliderNode.Value
            };

            var bindingMaxSlider = new Binding("Max")
            {
                Mode = BindingMode.OneWay,
                Source = sliderNode,
                UpdateSourceTrigger = UpdateSourceTrigger.Explicit
            };
            slider.SetBinding(RangeBase.MaximumProperty, bindingMaxSlider);

            var bindingMinSlider = new Binding("Min")
            {
                Mode = BindingMode.OneWay,
                Source = sliderNode,
                UpdateSourceTrigger = UpdateSourceTrigger.Explicit
            };
            slider.SetBinding(RangeBase.MinimumProperty, bindingMinSlider);

            var bindingValueSlider = new Binding("Value")
            {
                Mode = BindingMode.TwoWay,
                Source = sliderNode
            };
            slider.SetBinding(RangeBase.ValueProperty, bindingValueSlider);

            return slider;
        }
    }
}
