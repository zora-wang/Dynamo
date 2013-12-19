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
using Dynamo.FSchemeInterop;
using DynamoRevitStarter.Properties;
using RevitServices.Elements;
using RevitServices.Transactions;
using Dynamo.Utilities;

namespace DynamoRevitStarter
{
    [Transaction(Autodesk.Revit.Attributes.TransactionMode.Automatic)]
    [Regeneration(RegenerationOption.Manual)]
    public class DynamoRevitStarterApp : IExternalApplication
    {
        public static RevitServicesUpdater updater;
        public static ExecutionEnvironment env;

        public Result OnStartup(UIControlledApplication application)
        {
            SetupDynamoButton(application);

            RevitServices.Threading.IdlePromise.RegisterIdle(application);
            updater = new RevitServicesUpdater(application.ControlledApplication);
            TransactionManager.SetupManager(new DebugTransactionStrategy());
            env = new ExecutionEnvironment();

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
            AppDomain.CurrentDomain.AssemblyResolve += AssemblyHelper.ResolveAssemblyDynamically;

            var basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var assemblyPath = Path.Combine(basePath, "DynamoRevitDS.dll");
            var assembly = AssemblyHelper.LoadAssemblyFromStream(assemblyPath);

            if (assembly == null)
            {
                return Result.Failed;
            }

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
