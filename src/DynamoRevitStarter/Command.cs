using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Dynamo;
using Dynamo.Applications;
using Dynamo.FSchemeInterop;
using DynamoRevitStarter.Properties;
using DynamoRevitWorker;
using RevitServices.Elements;
using RevitServices.Transactions;
using Dynamo.Utilities;
using MessageBox = System.Windows.MessageBox;

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

    [Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class DynamoRevitStarterCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData revit, ref string message, ElementSet elements)
        {
            AppDomain.CurrentDomain.AssemblyResolve += AssemblyHelper.ResolveAssemblyDynamically;

            //Add an assembly load step for the System.Windows.Interactivity assembly
            //Revit owns a version of this as well. Adding our step here prevents a duplicative
            //load of the dll at a later time.
            var assLoc = Assembly.GetExecutingAssembly().Location;
            if (string.IsNullOrEmpty(assLoc))
            {
                assLoc = @"C:\Program Files\Autodesk\Revit Architecture 2014\";
            }
            var interactivityPath = Path.Combine(Path.GetDirectoryName(assLoc), "System.Windows.Interactivity.dll");
            var interactivityAss = Assembly.LoadFrom(interactivityPath);

            //When a user double-clicks the Dynamo icon, we need to make
            //sure that we don't create another instance of Dynamo.
            /*if (DynamoWorker.isRunning)
            {
                Debug.WriteLine("Dynamo is already running.");
                if (dynamoView != null)
                {
                    dynamoView.Focus();
                }
                return Result.Succeeded;
            }*/

            try
            {
                Debug.WriteLine("Creating Dynamo AppDomain.");
                var domainSetup = new AppDomainSetup {PrivateBinPath = string.Empty};
                domainSetup.PrivateBinPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                domainSetup.ApplicationBase = domainSetup.PrivateBinPath;
                domainSetup.ShadowCopyFiles = "true";
                domainSetup.ShadowCopyDirectories = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

                var dynamoDomain = AppDomain.CreateDomain("Dynamo", null, domainSetup);
                dynamoDomain.AssemblyResolve += AssemblyHelper.ResolveAssemblyDynamically;

                var remoteWorker = (Worker)dynamoDomain.CreateInstanceAndUnwrap(
                    "DynamoRevitWorker, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
                    "DynamoRevitWorker.Worker");
                remoteWorker.internalRevitData = revit;
                remoteWorker.env = DynamoRevitStarterApp.env;
                remoteWorker.updater = DynamoRevitStarterApp.updater;

                remoteWorker.DoDynamo();
            }
            catch (Exception ex)
            {
                Worker.isRunning = false;
                MessageBox.Show(ex.ToString());

                DynamoLogger.Instance.Log(ex.Message);
                DynamoLogger.Instance.Log(ex.StackTrace);
                DynamoLogger.Instance.Log("Dynamo log ended " + DateTime.Now.ToString(CultureInfo.InvariantCulture));

                return Result.Failed;
            }

            return Result.Succeeded;
        }
    }
}
