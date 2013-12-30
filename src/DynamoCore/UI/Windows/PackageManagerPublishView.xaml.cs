using System;
using System.Windows;
using Dynamo.Controls;
using Dynamo.UI.Views;
using Dynamo.Utilities;

namespace Dynamo.PackageManager
{
    /// <summary>
    /// Interaction logic for PackageManagerPublishView.xaml
    /// </summary>
    public partial class PackageManagerPublishView : Window, ISpecificVersionComponent
    {
        public PackageManagerPublishView(PublishPackageViewModel packageViewModel)
        {

            this.DataContext = packageViewModel;
            packageViewModel.PublishSuccess += PackageViewModelOnPublishSuccess;

            this.Owner = WPF.FindUpVisualTree<DynamoView>(this);
            this.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            LoadSpecificVersionComponent();

            InitializeComponent();
        }

        private void PackageViewModelOnPublishSuccess(PublishPackageViewModel sender)
        {
            this.Dispatcher.BeginInvoke((Action) (Close));
        }

        public void LoadSpecificVersionComponent()
        {
            _contentLoaded = true;
            SpecificVersionLoader.LoadSpecificVersionWindow(this);
        }
    }

}
