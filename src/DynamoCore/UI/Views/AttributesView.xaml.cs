using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Dynamo.Utilities;

namespace Dynamo.UI.Views
{
    /// <summary>
    /// Interaction logic for AttributesView.xaml
    /// </summary>
    public partial class AttributesView : UserControl
    {
        public AttributesView()
        {
            InitializeComponent();
        }

        private void Button_MouseEnter(object sender, MouseEventArgs e)
        {
            Button b = (Button)sender;
            Grid g = (Grid)b.Parent;
            Label lb = (Label)(g.Children[1]);
            var bc = new BrushConverter();
            lb.Foreground = (Brush)bc.ConvertFromString("#cccccc");
            Image collapsestate = (Image)(b).Content;
            var collapsestateSource = new Uri(@"pack://application:,,,/DynamoCore;component/UI/Images/expand_hover.png");
            BitmapImage bmi = new BitmapImage(collapsestateSource);
            RotateTransform rotateTransform = new RotateTransform(-90, 16, 16);
            collapsestate.Source = new BitmapImage(collapsestateSource);

            this.Cursor = CursorLibrary.GetCursor(CursorSet.LinkSelect);
        }

        private void buttonGrid_MouseLeave(object sender, MouseEventArgs e)
        {
            Button b = (Button)sender;
            Grid g = (Grid)b.Parent;
            Label lb = (Label)(g.Children[1]);
            var bc = new BrushConverter();
            lb.Foreground = (Brush)bc.ConvertFromString("#aaaaaa");
            Image collapsestate = (Image)(b).Content;
            var collapsestateSource = new Uri(@"pack://application:,,,/DynamoCore;component/UI/Images/expand_normal.png");
            collapsestate.Source = new BitmapImage(collapsestateSource);

            this.Cursor = null;
        }

        private void OnAttributesClick(object sender, RoutedEventArgs e)
        {
            dynSettings.Controller.DynamoViewModel.OnRightSidebarClosed(this, EventArgs.Empty);
        }
    }
}
