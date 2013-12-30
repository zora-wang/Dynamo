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
            SpecificVersionLoader.LoadSpecificVersionUserControl(this);
        }
    }
}