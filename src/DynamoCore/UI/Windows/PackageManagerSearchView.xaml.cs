using System.Windows;
using System.Windows.Controls;
using Dynamo.UI.Views;

namespace Dynamo.PackageManager.UI
{
    /// <summary>
    /// Interaction logic for PackageManagerSearchView.xaml
    /// </summary>
    public partial class PackageManagerSearchView : Window, ISpecificVersionComponent
    {
        public PackageManagerSearchView(PackageManagerSearchViewModel pm)
        {
            this.DataContext = pm;

            LoadSpecificVersionComponent();

            InitializeComponent();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            (this.DataContext as PackageManagerSearchViewModel).SearchAndUpdateResults(this.SearchTextBox.Text);
        }

        private void SortButton_OnClick(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            button.ContextMenu.DataContext = button.DataContext;
            button.ContextMenu.IsOpen = true;
        }

        public void LoadSpecificVersionComponent()
        {
            _contentLoaded = true;
            SpecificVersionLoader.LoadSpecificVersionWindow(this);
        }
    }
}
