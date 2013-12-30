using System;

namespace Dynamo.UI.Views
{
    interface ISpecificVersionComponent
    {
        void LoadSpecificVersionComponent();
    }

    public class SpecificVersionLoader
    {
        public static void LoadSpecificVersionUserControl(object component)
        {
            var assemblyName = component.GetType().Assembly.GetName();
            var uri =
                new Uri(
                    string.Format("/{0};v{1};component/ui/controls/{2}.xaml", assemblyName.Name, assemblyName.Version,
                        component.GetType().Name), UriKind.Relative);
            System.Windows.Application.LoadComponent(component, uri);
        }

        public static void LoadSpecificVersionWindow(object component)
        {
            var assemblyName = component.GetType().Assembly.GetName();
            var uri =
                new Uri(
                    string.Format("/{0};v{1};component/ui/windows/{2}.xaml", assemblyName.Name, assemblyName.Version,
                        component.GetType().Name), UriKind.Relative);
            System.Windows.Application.LoadComponent(component, uri);
        }
    }
}
