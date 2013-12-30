using System.Windows;
using System.Windows.Controls;
using Dynamo.UI.Views;
using Dynamo.Utilities;

namespace Dynamo.PackageManager.UI
{
    /// <summary>
    /// Interaction logic for DynamoInstalledPackagesView.xaml
    /// </summary>
    public partial class InstalledPackagesView : Window, ISpecificVersionComponent
    {
        public InstalledPackagesView()
        {
            this.DataContext = dynSettings.PackageLoader;

            LoadSpecificVersionComponent();

            InitializeComponent();
        }

        private void BrowseOnline_OnClick(object sender, RoutedEventArgs e)
        {
            dynSettings.PackageManagerClient.GoToWebsite();
        }

        private void MoreButton_OnClick(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            button.ContextMenu.DataContext = button.DataContext;
            button.ContextMenu.IsOpen = true;
            
            //if (e.LeftButton == MouseButtonState.Pressed)
            //{



            //}
        }

        public void LoadSpecificVersionComponent()
        {
            _contentLoaded = true;
            SpecificVersionLoader.LoadSpecificVersionWindow(this);
        }
    }
}
