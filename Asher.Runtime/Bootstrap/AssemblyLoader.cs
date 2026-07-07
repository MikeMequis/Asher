using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Asher.Runtime.Bootstrap
{
    public static class AssemblyLoader
    {
        public static void LoadAssembliesFrom(string directory)
        {
            if (!Directory.Exists(directory))
            {
                RuntimeLogger.Warning($"[Mods] Directory not found: {directory}");
                return;
            }

            var dllFiles = Directory.GetFiles(directory, "*.dll");
            var loaded = new List<string>();

            foreach (var dll in dllFiles)
            {
                var fileName = Path.GetFileName(dll);

                try
                {
                    Assembly.LoadFrom(dll);
                    loaded.Add(fileName);
                }
                catch (ReflectionTypeLoadException ex)
                {
                    RuntimeLogger.Error($"[Mods] Failed to load {fileName}");

                    if (ex.LoaderExceptions != null)
                    {
                        foreach (var loaderEx in ex.LoaderExceptions)
                        {
                            if (loaderEx != null)
                                RuntimeLogger.Error($"  - {loaderEx.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    RuntimeLogger.Warning($"[Mods] Failed to load {fileName}: {ex.Message}");
                }
            }

            if (loaded.Count == 0)
                RuntimeLogger.Info("[Mods] No mod assemblies loaded.");
            else
                RuntimeLogger.Info($"[Mods] Loaded {loaded.Count}: {string.Join(", ", loaded)}");
        }
    }
}
