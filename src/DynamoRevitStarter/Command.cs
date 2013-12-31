using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Dynamo.Applications;
using DynamoRevitStarter.Properties;
using Dynamo.Utilities;

namespace DynamoRevitStarter
{
    [Transaction(TransactionMode.Automatic)]
    [Regeneration(RegenerationOption.Manual)]
    public class DynamoRevitStarterApp : IExternalApplication
    {
        public static object updater;
        public static object env;

        public Result OnStartup(UIControlledApplication application)
        {
            AppDomain.CurrentDomain.AssemblyResolve += AssemblyHelper.ResolveAssemblyDynamically;

            SetupDynamoButton(application);

            var basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var servicesPath = Path.Combine(basePath, "RevitServices.dll");
            var interopPath = Path.Combine(basePath, "FSchemeInterop.dll");

            var servicesAssembly = AssemblyHelper.LoadAssemblyFromStream(servicesPath);
            var interopAssembly = AssemblyHelper.LoadAssemblyFromStream(interopPath);

            var idlePromiseType = servicesAssembly.GetType("RevitServices.Threading.IdlePromise");
            idlePromiseType.GetMethod("RegisterIdle").Invoke(null, new object[] {application});
            //RevitServices.Threading.IdlePromise.RegisterIdle(application);

            var updaterType = servicesAssembly.GetType("RevitServices.Elements.RevitServicesUpdater");
            updater = Activator.CreateInstance(updaterType, new object[] {application.ControlledApplication});
            //updater = new RevitServicesUpdater(application.ControlledApplication);

            var managerType = servicesAssembly.GetType("RevitServices.Transactions.TransactionManager");
            var strategyType = servicesAssembly.GetType("RevitServices.Transactions.DebugTransactionStrategy");
            var strategy = Activator.CreateInstance(strategyType);
            managerType.GetMethod("SetupManager", new [] { strategyType }).Invoke(null, new object[] { strategy });
            //TransactionManager.SetupManager(new DebugTransactionStrategy());

            Type envType = interopAssembly.GetType("Dynamo.FSchemeInterop.ExecutionEnvironment");
            env = Activator.CreateInstance(envType);
            //env = new ExecutionEnvironment();

            return Result.Succeeded;
        }

        private static void SetupDynamoButton(UIControlledApplication application)
        {
            //TAF load english_us TODO add a way to localize
            var res = Resource_en_us.ResourceManager;
            // Create new ribbon panel
            RibbonPanel ribbonPanel = application.CreateRibbonPanel(res.GetString("App_Description"));

            var assemblyName = string.IsNullOrEmpty(Assembly.GetExecutingAssembly().Location)
                ? new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath
                : Assembly.GetExecutingAssembly().Location;

            //Create a push button in the ribbon panel 
            var pushButton = ribbonPanel.AddItem(new PushButtonData("DynamoDS",
                res.GetString("App_Name"), assemblyName,
                "DynamoRevitStarter.DynamoRevitStarterCommand")) as
                PushButton;

            Bitmap dynamoIcon = Resources.logo_square_32x32;

            BitmapSource bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(
                dynamoIcon.GetHbitmap(),
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());

            pushButton.LargeImage = bitmapSource;
            pushButton.Image = bitmapSource;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
    }

    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class DynamoRevitStarterCommand : IExternalCommand
    {
        //https://code.google.com/p/revitpythonshell/wiki/FeaturedScriptLoadplugin
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            AssemblyHelper.LoadCoreAssembliesForRevitIfNewer();

            AppDomain.CurrentDomain.AssemblyResolve += AssemblyHelper.ResolveAssemblyDynamically;
            
            var assembly = AssemblyHelper.FindNewestVersionOfAssemblyByName("DynamoRevitDS");

            //create an instance of the DynamoRevit external command object
            //using reflection
            var type = assembly.GetType("Dynamo.Applications.DynamoRevit");
            var dynRevit = Activator.CreateInstance(type);

            //set some fields on the instance of the command
            var updaterField = type.GetField("updater");
            var envField = type.GetField("env");
            updaterField.SetValue(dynRevit, DynamoRevitStarterApp.updater);
            envField.SetValue(dynRevit, DynamoRevitStarterApp.env);

            //execute the command
            var method = type.GetMethod("Execute");
            method.Invoke(dynRevit, new object[] {commandData, message, elements});

            return Result.Succeeded;
        }

    }
}
