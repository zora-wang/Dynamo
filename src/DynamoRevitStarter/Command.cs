using System;
using System.IO;
using System.Reflection;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Dynamo.FSchemeInterop;
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
            RevitServices.Threading.IdlePromise.RegisterIdle(application);
            updater = new RevitServicesUpdater(application.ControlledApplication);
            TransactionManager.SetupManager(new DebugTransactionStrategy());
            env = new ExecutionEnvironment();

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            //throw new NotImplementedException();
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
