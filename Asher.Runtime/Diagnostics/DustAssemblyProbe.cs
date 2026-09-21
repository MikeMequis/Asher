using System;
using System.Collections.Generic;
using System.Reflection;

namespace Asher.Runtime.Diagnostics
{
    /// <summary>
    /// Reflection-only introspection of the managed assemblies already loaded by Dust.
    ///
    /// It enumerates AppDomain.CurrentDomain.GetAssemblies(), identifies the game assembly at
    /// runtime (no hardcoded type names), resolves one real type through Assembly.GetType and
    /// lists a few of its methods. It never invokes game code and never mutates state.
    /// </summary>
    public static class DustAssemblyProbe
    {
        private static readonly string[] FrameworkPrefixes =
        {
            "System",
            "mscorlib",
            "Mono.",
            "MonoGame",
            "Microsoft.",
            "netstandard",
            "Accessibility",
            "Asher.",
            "0Harmony",
            "Newtonsoft",
            "Windows.",
            "OpenTK",
            "FNA",
            "SDL"
        };

        private const int MaxMethodsReported = 5;

        /// <summary>
        /// Returns the loaded Dust game assembly, or null when it cannot be identified.
        /// Shared with the Harmony PoC so the target is resolved from the same runtime data.
        /// </summary>
        public static Assembly FindLoadedDustAssembly()
        {
            return FindDustAssembly(AppDomain.CurrentDomain.GetAssemblies());
        }

        public static void Run()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            Console.WriteLine($"[Asher] Loaded assemblies: {assemblies.Length}");

            foreach (var assembly in assemblies)
            {
                try
                {
                    Console.WriteLine($"[Asher] Assembly: {assembly.FullName}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Asher] Assembly: <unreadable: {ex.GetType().Name}: {ex.Message}>");
                }
            }

            var dust = FindDustAssembly(assemblies);
            if (dust == null)
            {
                Console.WriteLine("[Asher] Dust assembly not identified");
                throw new InvalidOperationException(
                    "Dust assembly could not be identified among the loaded assemblies");
            }

            Console.WriteLine($"[Asher] Dust assembly found: {dust.GetName().Name}");

            var types = GetTypesSafe(dust, reportLoaderExceptions: true);
            Console.WriteLine($"[Asher] Dust assembly types: {types.Count}");

            if (types.Count == 0)
            {
                throw new InvalidOperationException(
                    $"Dust assembly '{dust.GetName().Name}' exposes no loadable types");
            }

            var candidate = SelectRepresentativeType(types);
            var resolved = dust.GetType(candidate.FullName);
            if (resolved == null)
            {
                throw new InvalidOperationException(
                    $"Type '{candidate.FullName}' could not be resolved via Assembly.GetType");
            }

            Console.WriteLine($"[Asher] Dust type resolved: {resolved.FullName}");

            ReportMethods(resolved);
        }

        private static void ReportMethods(Type type)
        {
            MethodInfo[] methods;
            try
            {
                methods = type.GetMethods(
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Instance | BindingFlags.Static |
                    BindingFlags.DeclaredOnly);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Asher] Method enumeration failed: {ex.GetType().Name}: {ex.Message}");
                return;
            }

            if (methods.Length == 0)
            {
                Console.WriteLine("[Asher] Method found: (none declared)");
                return;
            }

            int shown = Math.Min(MaxMethodsReported, methods.Length);
            for (int i = 0; i < shown; i++)
            {
                Console.WriteLine($"[Asher] Method found: {methods[i].Name}");
            }

            if (methods.Length > shown)
            {
                Console.WriteLine($"[Asher] Method found: ... {methods.Length - shown} more not shown");
            }
        }

        private static Assembly FindDustAssembly(Assembly[] assemblies)
        {
            Assembly best = null;
            int bestScore = int.MinValue;

            foreach (var assembly in assemblies)
            {
                string name;
                try
                {
                    name = assembly.GetName().Name;
                }
                catch
                {
                    continue;
                }

                if (IsFramework(name))
                {
                    continue;
                }

                var types = GetTypesSafe(assembly, reportLoaderExceptions: false);

                int score = types.Count;
                foreach (var type in types)
                {
                    if (type == null)
                    {
                        continue;
                    }

                    if (type.Namespace != null &&
                        type.Namespace.StartsWith("Dust", StringComparison.Ordinal))
                    {
                        score += 100;
                    }

                    if (HasBaseTypeNamed(type, "Game"))
                    {
                        score += 1000;
                    }
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    best = assembly;
                }
            }

            return best;
        }

        private static bool IsFramework(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return true;
            }

            foreach (var prefix in FrameworkPrefixes)
            {
                if (name.StartsWith(prefix, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasBaseTypeNamed(Type type, string baseTypeName)
        {
            try
            {
                var current = type.BaseType;
                int guard = 0;
                while (current != null && guard++ < 64)
                {
                    if (current.Name == baseTypeName || current.FullName == baseTypeName)
                    {
                        return true;
                    }

                    current = current.BaseType;
                }
            }
            catch
            {
                // Reflection over a partially loaded type can throw; treat as no match.
            }

            return false;
        }

        private static Type SelectRepresentativeType(List<Type> types)
        {
            Type fallback = null;

            foreach (var type in types)
            {
                if (type == null)
                {
                    continue;
                }

                if (HasBaseTypeNamed(type, "Game"))
                {
                    return type;
                }

                if (fallback == null && type.IsPublic && !type.IsNested)
                {
                    fallback = type;
                }
            }

            if (fallback != null)
            {
                return fallback;
            }

            foreach (var type in types)
            {
                if (type != null && type.IsPublic && !type.IsNested)
                {
                    return type;
                }
            }

            return types[0];
        }

        private static List<Type> GetTypesSafe(Assembly assembly, bool reportLoaderExceptions)
        {
            try
            {
                return new List<Type>(assembly.GetTypes());
            }
            catch (ReflectionTypeLoadException ex)
            {
                if (reportLoaderExceptions)
                {
                    Console.WriteLine(
                        $"[Asher] GetTypes threw ReflectionTypeLoadException in {assembly.GetName().Name}");

                    if (ex.LoaderExceptions != null)
                    {
                        for (int i = 0; i < ex.LoaderExceptions.Length; i++)
                        {
                            var loaderException = ex.LoaderExceptions[i];
                            Console.WriteLine(
                                $"[Asher] Loader exception[{i}]: " +
                                (loaderException == null ? "(null)" : loaderException.Message));
                        }
                    }
                }

                var loaded = new List<Type>();
                if (ex.Types != null)
                {
                    foreach (var type in ex.Types)
                    {
                        if (type != null)
                        {
                            loaded.Add(type);
                        }
                    }
                }

                return loaded;
            }
            catch (Exception ex)
            {
                if (reportLoaderExceptions)
                {
                    Console.WriteLine(
                        $"[Asher] GetTypes failed in {assembly.GetName().Name}: " +
                        $"{ex.GetType().Name}: {ex.Message}");
                }

                return new List<Type>();
            }
        }
    }
}
