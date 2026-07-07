using Asher.SDK.Logging;
using Asher.SDK.Patching;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Asher.Patching.ContentPatcher
{
    /// <summary>
    /// Intercepts XNA ContentManager asset loading and swaps assets from Asher/patches/content.json.
    /// </summary>
    public sealed class ContentPatcherPatch : IAsherPatchModule
    {
        private static readonly (string TypeName, string AssemblyFile)[] KnownAssetTypes =
        {
            ("Microsoft.Xna.Framework.Graphics.Texture2D", "Microsoft.Xna.Framework.Graphics.dll"),
            ("Microsoft.Xna.Framework.Graphics.SpriteFont", "Microsoft.Xna.Framework.Graphics.dll"),
            ("Microsoft.Xna.Framework.Graphics.Effect", "Microsoft.Xna.Framework.Graphics.dll"),
            ("Microsoft.Xna.Framework.Media.Song", "Microsoft.Xna.Framework.dll"),
            ("Microsoft.Xna.Framework.Media.Video", "Microsoft.Xna.Framework.Video.dll"),
        };

        public static bool Enabled { get; set; }

        public string Name => "Content Patcher";

        public void Apply(Harmony harmony)
        {
            if (!Enabled)
                return;

            try
            {
                var gameFolder = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\', '/');
                ContentPatchRegistry.Initialize(gameFolder);

                var contentManagerType = GetContentManagerType();
                if (contentManagerType == null)
                {
                    AsherLog.Warning("[ContentPatcher] ContentManager type not found");
                    return;
                }

                var hooked = TryApplyHooks(harmony, contentManagerType);
                if (hooked == 0)
                {
                    LogCandidateMethods(contentManagerType);
                    AsherLog.Warning("[ContentPatcher] No compatible ContentManager load methods found");
                    return;
                }

                AsherLog.Info($"[ContentPatcher] Applied {hooked} hook(s) with {ContentPatchRegistry.ReplacementCount} replacement(s)");
            }
            catch (Exception ex)
            {
                AsherLog.Error($"[ContentPatcher] Failed to apply patch: {ex.Message}");
                if (ex.InnerException != null)
                    AsherLog.Error($"[ContentPatcher] Inner: {ex.InnerException.Message}");
            }
        }

        private static int TryApplyHooks(Harmony harmony, Type contentManagerType)
        {
            int hooked = 0;

            var loadAssetMethod = FindLoadAssetMethod(contentManagerType);
            if (loadAssetMethod != null)
            {
                harmony.Patch(
                    loadAssetMethod,
                    prefix: new HarmonyMethod(typeof(ContentPatcherPatch), nameof(LoadAssetPrefix)));
                AsherLog.Info($"[ContentPatcher] Hooked {loadAssetMethod.Name}");
                return 1;
            }

            var genericPrefix = new HarmonyMethod(typeof(ContentPatcherPatch), nameof(LoadGenericPrefix));
            foreach (var method in FindConcreteGenericLoadMethods(contentManagerType))
            {
                harmony.Patch(method, prefix: genericPrefix);
                hooked++;
            }

            if (hooked > 0)
                AsherLog.Info($"[ContentPatcher] Hooked {hooked} closed generic load method(s)");

            return hooked;
        }

        private static bool LoadAssetPrefix(
            string originalAssetName,
            Type assetType,
            ref object __result,
            object __instance)
        {
            return TryReplaceAsset(originalAssetName, assetType, ref __result, __instance);
        }

        private static bool LoadGenericPrefix(
            string assetName,
            ref object __result,
            object __instance,
            MethodBase __originalMethod)
        {
            if (!Enabled || __originalMethod == null)
                return true;

            var assetType = __originalMethod.GetGenericArguments()[0];
            return TryReplaceAsset(assetName, assetType, ref __result, __instance);
        }

        private static bool TryReplaceAsset(
            string assetName,
            Type assetType,
            ref object __result,
            object contentManager)
        {
            if (!Enabled)
                return true;

            try
            {
                if (!ContentPatchRegistry.TryGetReplacement(assetName, out _, out var filePath))
                    return true;

                var replacement = ContentAssetLoader.TryLoadReplacement(contentManager, assetType, filePath);
                if (replacement == null)
                    return true;

                __result = replacement;
                return false;
            }
            catch (Exception ex)
            {
                AsherLog.Error($"[ContentPatcher] Failed to load replacement for '{assetName}': {ex.Message}");
                return true;
            }
        }

        private static MethodInfo FindLoadAssetMethod(Type contentManagerType)
        {
            return contentManagerType
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault(method =>
                    method.Name == "LoadAsset"
                    && !method.IsGenericMethodDefinition
                    && method.GetParameters().Length == 3
                    && method.GetParameters()[0].ParameterType == typeof(string)
                    && method.GetParameters()[1].ParameterType == typeof(Type)
                    && method.ReturnType == typeof(object));
        }

        private static IEnumerable<MethodInfo> FindConcreteGenericLoadMethods(Type contentManagerType)
        {
            var results = new List<MethodInfo>();
            var assetTypes = ResolveAssetTypes().ToList();
            if (assetTypes.Count == 0)
                return results;

            var genericMethods = contentManagerType
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(method =>
                    method.IsGenericMethodDefinition
                    && (method.Name == "Load" || method.Name == "ReadAsset")
                    && method.GetParameters().Length >= 1
                    && method.GetParameters()[0].ParameterType == typeof(string))
                .ToList();

            foreach (var genericMethod in genericMethods)
            {
                foreach (var assetType in assetTypes)
                {
                    try
                    {
                        results.Add(genericMethod.MakeGenericMethod(assetType));
                    }
                    catch
                    {
                        // Type not supported by this generic method.
                    }
                }
            }

            return results;
        }

        private static IEnumerable<Type> ResolveAssetTypes()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var seen = new HashSet<Type>();

            foreach (var (typeName, assemblyFile) in KnownAssetTypes)
            {
                var type = ResolveType(baseDir, typeName, assemblyFile);
                if (type != null && seen.Add(type))
                    yield return type;
            }
        }

        private static Type ResolveType(string baseDir, string typeName, string assemblyFile)
        {
            var assemblyPath = Path.Combine(baseDir, assemblyFile);
            if (File.Exists(assemblyPath))
            {
                var type = Assembly.LoadFrom(assemblyPath).GetType(typeName);
                if (type != null)
                    return type;
            }

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var type = assembly.GetType(typeName, false);
                    if (type != null)
                        return type;
                }
                catch
                {
                    // Ignore broken assembly lookups.
                }
            }

            return null;
        }

        private static void LogCandidateMethods(Type contentManagerType)
        {
            var names = contentManagerType
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(method =>
                    method.Name.IndexOf("Load", StringComparison.OrdinalIgnoreCase) >= 0
                    || method.Name.IndexOf("Read", StringComparison.OrdinalIgnoreCase) >= 0
                    || method.Name.IndexOf("Asset", StringComparison.OrdinalIgnoreCase) >= 0)
                .Select(method =>
                {
                    var parameters = string.Join(", ", method.GetParameters().Select(p => p.ParameterType.Name));
                    var generic = method.IsGenericMethodDefinition ? "<T>" : string.Empty;
                    return method.Name + generic + "(" + parameters + ")";
                })
                .Distinct()
                .Take(25);

            AsherLog.Warning("[ContentPatcher] Candidate methods: " + string.Join("; ", names));
        }

        private static Type GetContentManagerType()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var xnaPath = Path.Combine(baseDir, "Microsoft.Xna.Framework.dll");
            if (File.Exists(xnaPath))
            {
                var type = Assembly.LoadFrom(xnaPath)
                    .GetType("Microsoft.Xna.Framework.Content.ContentManager");
                if (type != null)
                    return type;
            }

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType("Microsoft.Xna.Framework.Content.ContentManager");
                if (type != null)
                    return type;
            }

            return null;
        }
    }
}
