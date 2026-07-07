using Asher.SDK.Patching;
using System;
using System.Linq;
using System.Reflection;

namespace Asher.Runtime.Bootstrap
{
    public static class PreInitBootstrap
    {
        public static void ExecutePreInitModules()
        {
            int modulesFound = 0;
            int modulesExecuted = 0;

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;

                try
                {
                    types = asm.GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    RuntimeLogger.Warning($"[PreInit] Failed to load types from {asm.GetName().Name}");
                    types = e.Types.Where(t => t != null).ToArray()!;
                }
                catch (Exception ex)
                {
                    RuntimeLogger.Error($"[PreInit] Error reading {asm.GetName().Name}: {ex.Message}");
                    continue;
                }

                foreach (var type in types)
                {
                    if (type == null)
                        continue;

                    if (!typeof(IAsherPreInitModule).IsAssignableFrom(type) ||
                        type.IsAbstract ||
                        type.IsInterface)
                        continue;

                    modulesFound++;

                    try
                    {
                        var module = (IAsherPreInitModule)Activator.CreateInstance(type)!;
                        module.Execute();
                        modulesExecuted++;
                    }
                    catch (Exception ex)
                    {
                        RuntimeLogger.Error(
                            $"[PreInit] Failed to run {type.Name}: {ex.Message}",
                            ex);
                    }
                }
            }

            RuntimeLogger.Info($"[PreInit] {modulesExecuted}/{modulesFound} modules executed.");
        }
    }
}
