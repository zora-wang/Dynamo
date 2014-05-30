using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using Microsoft.Practices.Prism.ViewModel;

namespace Dynamo.ViewModels
{
    public class AttributesViewModel : NotificationObject
    {
        private ObservableCollection<AttributeCategory> _categories;

        public ObservableCollection<AttributeCategory> Categories
        {
            get { return _categories; }
            set
            {
                _categories = value;
                RaisePropertyChanged("Categories");
            }
        } 

        public AttributesViewModel()
        {
            Categories = new ObservableCollection<AttributeCategory>();
        }
    }

    public interface IAttribute { }

    public class AttributeCategory : IAttribute
    {
        public string Name { get; private set; }

        public IEnumerable<IAttribute> Attributes { get; set; }

        public AttributeCategory(string name, IEnumerable<IAttribute> attributes)
        {
            Name = name;
            Attributes = attributes;
        }
    }

    public class Attribute : IAttribute
    {
        public Attribute(String name, UIElement element)
        {
            Name = name;
            Element = element;
        }

        public string Name { get; set; }

        public UIElement Element
        {
            get; private set;
        }
    }
}
