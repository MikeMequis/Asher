using Asher.ContentPatcher;
using Asher.SDK.Logging;
using Asher.SDK.Patching;
using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;

namespace Asher.Patching.ContentPatcher
{
    /// <summary>
    /// Intercepts XNA ContentManager.Load calls and swaps assets defined in Asher/patches/content.json.
    /// </summary>
    public sealed class ContentPatcherPatch : IAsherPatchModule
    {
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

                var loadMethods = contentManagerType
                    .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                    .Where(method => method.Name == "Load" && method.IsGenericMethodDefinition)
                    .ToArray();

                if (loadMethods.Length == 0)
                {
                    AsherLog.Warning("[ContentPatcher] ContentManager.Load methods not found");
                    return;
                }

                var prefix = new HarmonyMethod(typeof(ContentPatcherPatch), nameof(LoadPrefix));

                foreach (var method in loadMethods)
                    harmony.Patch(method, prefix: prefix);
            }
            catch (Exception ex)
            {
                AsherLog.Error($"[ContentPatcher] Failed to apply patch: {ex.Message}");
            }
        }

        private static bool LoadPrefix(
            string assetName,
            ref object __result,
            object __instance,
            MethodBase __originalMethod)
        {
            if (!Enabled)
                return true;

            try
            {
                if (!ContentPatchRegistry.TryGetReplacement(assetName, out ContentPatchEntry entry, out var filePath))
                    return true;

                var assetType = __originalMethod.GetGenericArguments()[0];
                var replacement = ContentAssetLoader.TryLoadReplacement(__instance, assetType, filePath);
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

        private static Type GetContentManagerType()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType("Microsoft.Xna.Framework.Content.ContentManager");
                if (type != null)
                    return type;
            }

            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var xnaPath = System.IO.Path.Combine(baseDir, "Microsoft.Xna.Framework.dll");
            if (!System.IO.File.Exists(xnaPath))
                return null;

            var xnaAssembly = Assembly.LoadFrom(xnaPath);
            return xnaAssembly.GetType("Microsoft.Xna.Framework.Content.ContentManager");
        }
    }
}
