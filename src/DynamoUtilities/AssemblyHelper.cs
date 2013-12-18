using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Diagnostics;

namespace Dynamo.Utilities
{
    public static class AssemblyHelper
    {
        /// <summary>
        /// Attempts to resolve an assembly from the dll directory.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            string folderPath = String.Empty;
            folderPath = String.IsNullOrEmpty(Assembly.GetExecutingAssembly().Location)?
                Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath):
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string assemblyPath = Path.Combine(folderPath  + @"\dll", new AssemblyName(args.Name).Name + ".dll");
            if (!File.Exists(assemblyPath))
                return null;
            Assembly assembly = Assembly.LoadFrom(assemblyPath);
            return assembly;
        }

        public static Version GetDynamoVersion()
        {
            var assembly = Assembly.GetCallingAssembly();
            return assembly.GetName().Version;
        }

        public static Assembly LoadLibG()
        {
            var libG = Assembly.LoadFrom(GetLibGPath());
            return libG;
        }

        public static string GetLibGPath()
        {
            string dll_dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\dll";
            string libGPath = Path.Combine(dll_dir, "LibGNet.dll");
            return libGPath;
        }

        public static Assembly LoadAssemblyFromStream(string assemblyPath)
        {
            var assemblyBytes = File.ReadAllBytes(assemblyPath);
            var pdbPath = Path.Combine(Path.GetDirectoryName(assemblyPath),
                Path.GetFileNameWithoutExtension(assemblyPath) + ".pdb");

            Assembly assembly = null;

            if (File.Exists(pdbPath))
            {
                var pdbBytes = File.ReadAllBytes(pdbPath);
                assembly = Assembly.Load(assemblyBytes, pdbBytes);
            }
            else
            {
                assembly = Assembly.Load(assemblyBytes);
            }
            return assembly;
        }

        public static object CreateInstanceByNameFromCore(string typeName)
        {
            string basePath = String.Empty;
            basePath = String.IsNullOrEmpty(Assembly.GetExecutingAssembly().Location)
                ? Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath)
                : Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            var corePath = Path.Combine(basePath, "DynamoCore.dll");
            var coreAssembly = AssemblyHelper.LoadAssemblyFromStream(corePath);

            var objType = coreAssembly.GetType(typeName);
            var obj = Activator.CreateInstance(objType);

            return obj;
        }
    
        public static Assembly ResolveAssemblyDynamically(object sender, ResolveEventArgs args)
        {
            Debug.WriteLine(string.Format("Attempting to resolve:{0}",args.Name));

            var name = args.Name.Split(',')[0];

            //find if the assembly is already loaded in the app domain
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            Assembly found = assemblies.FirstOrDefault(x => x.FullName == args.Name);

            if (found != null)
            {
                return found;
            }

            Assembly assembly = null;
            try
            {
                //get the folder to load dlls from
                var folder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var dllPath = Path.Combine(folder, name + ".dll");

                if (!File.Exists(dllPath))
                    return null;

                assembly = LoadAssemblyFromStream(dllPath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
                return null;
            }

            Debug.WriteLine("Resolved assembly:" + args.Name);
            return assembly;
        }
  
    }
}
