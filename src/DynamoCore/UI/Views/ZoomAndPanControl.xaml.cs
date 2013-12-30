using System;
using System.Windows.Controls;
using Dynamo.UI.Views;
using Dynamo.ViewModels;

namespace Dynamo.Controls
{
    /// <summary>
    /// Interaction logic for ZoomAndPanControl.xaml
    /// </summary>
    public partial class ZoomAndPanControl : UserControl, ISpecificVersionComponent
    {
        public ZoomAndPanControl(WorkspaceViewModel workspaceViewModel)
        {
            LoadSpecificVersionComponent();

            InitializeComponent();
            this.DataContext = workspaceViewModel;
        }

        public void LoadSpecificVersionComponent()
        {
            _contentLoaded = true;
            var assemblyName = GetType().Assembly.GetName();
            var uri =
                new Uri(
                    string.Format("/{0};v{1};component/ui/views/{2}.xaml", assemblyName.Name, assemblyName.Version,
                        GetType().Name), UriKind.Relative);
            System.Windows.Application.LoadComponent(this, uri);
        }
    }
}