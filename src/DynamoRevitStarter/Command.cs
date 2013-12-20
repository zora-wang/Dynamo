using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
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
            AppDomain.CurrentDomain.AssemblyResolve += ResolveAssemblyDynamically;

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
                var domainSetup = new AppDomainSetup
                {
                    PrivateBinPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                    ApplicationBase = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                    ShadowCopyFiles = "true",
                    ShadowCopyDirectories = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                    ApplicationName = "DynamoRevit",
                    CachePath =
                        Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "shadow_copies"),
                    LoaderOptimization = LoaderOptimization.MultiDomain
                };

                var dynamoDomain = AppDomain.CreateDomain("Dynamo", null, domainSetup);
                
                var t = typeof (Proxy);
                var proxy = (Proxy)dynamoDomain.CreateInstanceAndUnwrap(t.Assembly.FullName, t.Namespace + "." + t.Name);
                proxy.RevitPath = Path.GetDirectoryName(Assembly.GetCallingAssembly().Location);
                proxy.LocalPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

                proxy.LoadAssemblies();
                proxy.DoWork();

            }
            catch (Exception ex)
            {
                //Worker.isRunning = false;
                MessageBox.Show(ex.ToString());

                //DynamoLogger.Instance.Log(ex.Message);
                //DynamoLogger.Instance.Log(ex.StackTrace);
                //DynamoLogger.Instance.Log("Dynamo log ended " + DateTime.Now.ToString(CultureInfo.InvariantCulture));

                return Result.Failed;
            }

            return Result.Succeeded;
        }

        public Assembly ResolveAssemblyDynamically(object sender, ResolveEventArgs args)
        {
            Debug.WriteLine(string.Format("{0} requesting attempt to resolve:{1}", args.RequestingAssembly, args.Name));

            var name = args.Name.Split(',')[0];

            //Find if the assembly is already loaded in the app domain.
            //We find the assembly by name, disregarding the version number.
            //This will have the effect of only loading the first version of an assembly 
            //that is requested.
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            Assembly found = assemblies.FirstOrDefault(x => x.FullName.Split(',')[0] == name);

            if (found != null)
            {
                return found;
            }

            //The assembly has not already been loaded. Attempt to load the assembly
            //looking first in the executing assembly's directory, then in the /dll sub-directory.
            Assembly assembly = null;
            try
            {
                //get the folder to load dlls from
                var folder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var dllPath = Path.Combine(folder, name + ".dll");
                var dllSubPath = Path.Combine(folder + @"\dll", name + ".dll");

                if (File.Exists(dllPath))
                {
                    assembly = AssemblyHelper.LoadAssemblyFromStream(dllPath);
                }
                else if (File.Exists(dllSubPath))
                {
                    assembly = AssemblyHelper.LoadAssemblyFromStream(dllSubPath);
                }
                else
                    return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
                return null;
            }

            AssemblyHelper.DebugDynamoCoreInstances();
            Debug.WriteLine(string.Format("Resolved assembly:{0} in {1}", args.Name, AppDomain.CurrentDomain.FriendlyName));
            return assembly;
        }
    }

    [Serializable]
    public class Proxy:MarshalByRefObject
    {
        public string RevitPath { get; set; }
        public string LocalPath { get; set; }

        public ExternalCommandData RevitData { get; set; }

        public void LoadAssemblies()
        {
            var dirInfo = new DirectoryInfo(LocalPath);

            foreach(var file in dirInfo.GetFiles("*.dll"))
            {
                if (file.Name == "DynamoRevitStarter.dll")
                    continue;
                Assembly.LoadFrom(file.FullName);
            }

            //var api = Assembly.LoadFrom(@"C:\Program Files\Autodesk\Revit Architecture 2014\RevitAPI.dll");
            //var apiui = Assembly.LoadFrom(@"C:\Program Files\Autodesk\Revit Architecture 2014\RevitAPIUI.dll");
        }

        public IEnumerable<Assembly> GetAssemblies()
        {
            return AppDomain.CurrentDomain.GetAssemblies();
        }

        public Assembly ResolveAssembly(object sender, ResolveEventArgs arg)
        {
            Debug.WriteLine(string.Format("Looking for assembly {0}", arg.Name));

            var name = arg.Name.Split(',')[0];

            //first check if the assembly is already loaded
            //if so, return the loaded assembly
            var assemblies = AppDomain.CurrentDomain.GetAssemblies().ToList();
            var found = assemblies.FirstOrDefault(x => x.FullName.Split(',')[0] == name);

            if (found != null)
            {
                Debug.WriteLine(string.Format("{0} found in {1}", arg.Name, AppDomain.CurrentDomain.FriendlyName));
                return found;
            }

            //check the revit path
            var dllPath = Path.Combine(RevitPath, name + ".dll");
            if (File.Exists(dllPath))
            {
                var assembly = Assembly.LoadFrom(dllPath);
                if (assembly != null)
                {
                    Debug.WriteLine(string.Format("Loaded {0} from Revit path in {1}.", arg.Name, AppDomain.CurrentDomain.FriendlyName));
                    return assembly;
                }
            }

            //check the local path
            dllPath = Path.Combine(LocalPath, name + ".dll");
            if (File.Exists(dllPath))
            {
                var assembly = Assembly.LoadFrom(dllPath);
                if (assembly != null)
                {
                    Debug.WriteLine(string.Format("Loaded {0} from local path in {1}.", arg.Name, AppDomain.CurrentDomain.FriendlyName));
                    return assembly;
                }
            }
            

            return null;
        }

        private static Assembly LoadAssembly(string dllPath)
        {
            if (!File.Exists(dllPath))
            {
                return null;
            }

            var assemblyBytes = File.ReadAllBytes(dllPath);
            var pdbPath = Path.Combine(Path.GetDirectoryName(dllPath),
                Path.GetFileNameWithoutExtension(dllPath) + ".pdb");

            Assembly assembly = null;

            if (File.Exists(pdbPath))
            {
                var pdbBytes = File.ReadAllBytes(pdbPath);
                assembly = AppDomain.CurrentDomain.Load(assemblyBytes, pdbBytes);
            }
            else
            {
                assembly = AppDomain.CurrentDomain.Load(assemblyBytes);
            }
            return assembly;
        }

        public void DoWork()
        {
            var remoteWorker = AppDomain.CurrentDomain.CreateInstanceAndUnwrap(
                    "DynamoRevitWorker, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
                    "DynamoRevitWorker.Worker");
            //remoteWorker.GetType().GetField("internalRevitData").SetValue(remoteWorker, RevitData);
            remoteWorker.GetType().GetField("env").SetValue(remoteWorker, DynamoRevitStarterApp.env);
            remoteWorker.GetType().GetField("updater").SetValue(remoteWorker, DynamoRevitStarterApp.updater);
            remoteWorker.GetType().GetMethod("DoDynamo").Invoke(remoteWorker, new object[] { });
        }
    }
}
