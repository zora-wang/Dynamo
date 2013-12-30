using Dynamo.UI.Commands;
using Dynamo.UI.Views;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace Dynamo.UI.Controls
{
    /// <summary>
    /// Interaction logic for ShortcutToolbar.xaml
    /// </summary>
    public partial class ShortcutToolbar : UserControl, ISpecificVersionComponent
    {
        private ObservableCollection<ShortcutBarItem> shortcutBarItems;
        public ObservableCollection<ShortcutBarItem> ShortcutBarItems
        {
            get { return shortcutBarItems; }
        }
        private ObservableCollection<ShortcutBarItem> shortcutBarRightSideItems;
        public ObservableCollection<ShortcutBarItem> ShortcutBarRightSideItems
        {
            get { return shortcutBarRightSideItems; }
        }

        public ShortcutToolbar()
        {
            shortcutBarItems = new ObservableCollection<ShortcutBarItem>();
            shortcutBarRightSideItems = new ObservableCollection<ShortcutBarItem>();

            LoadSpecificVersionComponent();

            InitializeComponent();
        }

        public void LoadSpecificVersionComponent()
        {
            _contentLoaded = true;
            var assemblyName = GetType().Assembly.GetName();
            var uri =
                new Uri(
                    string.Format("/{0};v{1};component/ui/controls/{2}.xaml", assemblyName.Name, assemblyName.Version,
                        GetType().Name), UriKind.Relative);
            Application.LoadComponent(this, uri);
        }
    }

    public partial class ShortcutBarItem
    {
        private string shortcutToolTip;
        private string imgNormalSource;
        private string imgHoverSource;
        private string imgDisabledSource;
        private DelegateCommand shortcutCommand;
        private string shortcutCommandParameter;

        public string ShortcutCommandParameter
        {
            get { return shortcutCommandParameter; }
            set { shortcutCommandParameter = value; }
        }

        public DelegateCommand ShortcutCommand
        {
            get { return shortcutCommand; }
            set { shortcutCommand = value; }
        }

        public string ImgDisabledSource
        {
            get { return imgDisabledSource; }
            set { imgDisabledSource = value; }
        }

        public string ImgHoverSource
        {
            get { return imgHoverSource; }
            set { imgHoverSource = value; }
        }

        public string ImgNormalSource
        {
            get { return imgNormalSource; }
            set { imgNormalSource = value; }
        }

        public string ShortcutToolTip
        {
            get { return shortcutToolTip; }
            set { shortcutToolTip = value; }
        }

        public bool IsEnabled
        {
            get
            {
                if (this.shortcutCommand != null)
                    return this.shortcutCommand.CanExecute(null);

                return false;
            }
        }
    }
}
