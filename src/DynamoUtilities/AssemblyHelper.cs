using System;
using System.Collections.Generic;
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
        /*public static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
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
        }*/

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

        /// <summary>
        /// Create an instance of an object from DynamoCore.
        /// </summary>
        /// <param name="typeName"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Count the number of DynamoCore assemblies that are loaded and write to debug.
        /// </summary>
        public static void DebugDynamoCoreInstances()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            try
            {
                var cores = assemblies.Where(x => x.FullName.Split(',')[0] == "DynamoCore");
                Debug.WriteLine(string.Format("There are {0} DynamoCore assemblies loaded.", cores.Count()));
            }
            catch
            {
            }
        }

        /// <summary>
        /// Assembly resolution callback. Resolves assemblies, by loading them from 
        /// byte arrays. Allows dynamic reloading of assemblies.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static Assembly ResolveAssemblyDynamically(object sender, ResolveEventArgs args)
        {
            var name = args.Name.Split(',')[0];

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            Assembly found = assemblies.FirstOrDefault(x => x.GetName().Name == name);

            if (found != null)
            {
                var version = new Version(args.Name.Split(',')[1].Split('=')[1]);
                if (found.GetName().Version >= version)
                {
                    return found;
                }
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
                    assembly = LoadAssemblyFromStream(dllPath);
                }
                else if (File.Exists(dllSubPath))
                {
                    assembly = LoadAssemblyFromStream(dllSubPath);
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

            //DebugDynamoCoreInstances();
            Debug.WriteLine("Resolved assembly:" + args.Name);
            return assembly;
        }

        /// <summary>
        /// Load an assembly from a byte array.
        /// </summary>
        /// <param name="assemblyPath"></param>
        /// <returns></returns>
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

        public static void LoadCoreAssembliesForRevitIfNewer()
        {
            var dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            var dlls = new List<string>
            {
                "System.Windows.Interactivity.dll",
                "Microsoft.Practices.Prism.dll",
                "DynamoCore.dll",
                "DynamoPython.dll",
                "DynamoRevitDS.dll",
                "DynamoWatch3D.dll",
                "DynamoUtilities.dll",
                "FScheme.dll",
                "FSchemeInterop.dll",
                "Greg.dll",
                "RevitServices.dll",
                "GraphToDSCompiler.dll",
                "ProtoCore.dll",
                "ProtoAssociative.dll",
                "ProtoImperative.dll",
                "ProtoInterface.dll",
                "ProtoScript.dll",
                "MIConvexHullPlugin.dll",
                "Newtonsoft.Json.dll",
                "RestSharp.dll"
            };

            LoadListOfDlls(dlls, dir);
        }

        public static void LoadCoreAssembliesIfNewer()
        {
            var dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            var dlls = new List<string>
            {
                "System.Windows.Interactivity.dll",
                "Microsoft.Practices.Prism.dll",
                "DynamoCore.dll",
                "DynamoPython.dll",
                "DynamoWatch3D.dll",
                "DynamoUtilities.dll",
                "FScheme.dll",
                "FSchemeInterop.dll",
                "Greg.dll",
                "GraphToDSCompiler.dll",
                "ProtoCore.dll",
                "ProtoAssociative.dll",
                "ProtoImperative.dll",
                "ProtoInterface.dll",
                "ProtoScript.dll",
                "MIConvexHullPlugin.dll",
                "Newtonsoft.Json.dll",
                "RestSharp.dll"
            };

            LoadListOfDlls(dlls, dir);
        }

        private static void LoadListOfDlls(List<string> dlls, string dir)
        {
            foreach (var fileName in dlls)
            {
                try
                {
                    var fullName = Path.Combine(dir, fileName);

                    if (!File.Exists(fullName))
                    {
                        continue;
                    }

                    var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                    Assembly found =
                        assemblies.FirstOrDefault(x => x.FullName.Split(',')[0] == Path.GetFileNameWithoutExtension(fileName));

                    if (found != null)
                    {
                        var foundVersion = found.GetName().Version;

                        var dllVersion = FileVersionInfo.GetVersionInfo(fullName).FileVersion == null
                            ? new Version()
                            : new Version(FileVersionInfo.GetVersionInfo(fullName).FileVersion);

                        if (dllVersion > foundVersion)
                        {
                            Debug.WriteLine(string.Format("Loading updated version {1} for {0}", dllVersion,
                                found.FullName));
                            LoadAssemblyFromStream(fullName);
                        }
                        else
                        {
                            Debug.WriteLine(string.Format("Existing version of {0} already loaded.", found.FullName));
                        }
                    }
                    else
                    {
                        Debug.WriteLine(string.Format("Loading first version of {0}", fullName));
                        LoadAssemblyFromStream(fullName);
                    }
                }
                catch
                {
                    continue;
                }
            }
        }

        public static Assembly FindNewestVersionOfAssemblyByName(string name)
        {
            return AppDomain.CurrentDomain.GetAssemblies().
                Where(x => x.FullName.Split(',')[0] == name).
                OrderByDescending(x=>new Version(x.FullName.Split(',')[1].Split('=')[1])).
                First();
        }

        public static IEnumerable<Assembly> GetLatestAssembliesInCurrentAppDomain()
        {
            var latest = from a in AppDomain.CurrentDomain.GetAssemblies()
                group a by a.FullName.Split(',')[0]
                into grp
                select grp.OrderByDescending(a => a.FullName.Split(',')[1].Split('=')[1]).FirstOrDefault();

            return latest;
        }

        public static string GetAssemblyLocation(Assembly assembly)
        {
            if (!string.IsNullOrEmpty(assembly.Location))
            {
                return assembly.Location;
            }

            //look in the Dynamo Directory
            var dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var loc = Path.Combine(dir, assembly.GetName().Name + ".dll");

            return loc;
        }
    }
}
