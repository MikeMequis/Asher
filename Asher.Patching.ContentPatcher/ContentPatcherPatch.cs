using Asher.SDK.Logging;
using Asher.SDK.Patching;
using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;

namespace Asher.Patching.ContentPatcher
{
    /// <summary>
    /// Intercepts XNA ContentManager.LoadAsset and swaps assets from Asher/patches/content.json.
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

                var loadAssetMethod = FindLoadAssetMethod(contentManagerType);
                if (loadAssetMethod == null)
                {
                    AsherLog.Warning("[ContentPatcher] ContentManager.LoadAsset method not found");
                    return;
                }

                harmony.Patch(
                    loadAssetMethod,
                    prefix: new HarmonyMethod(typeof(ContentPatcherPatch), nameof(LoadAssetPrefix)));

                AsherLog.Info($"[ContentPatcher] Applied with {ContentPatchRegistry.ReplacementCount} replacement(s)");
            }
            catch (Exception ex)
            {
                AsherLog.Error($"[ContentPatcher] Failed to apply patch: {ex.Message}");
                if (ex.InnerException != null)
                    AsherLog.Error($"[ContentPatcher] Inner: {ex.InnerException.Message}");
            }
        }

        /// <summary>
        /// Harmony cannot patch open generic Load&lt;T&gt;; LoadAsset is the non-generic entry point.
        /// </summary>
        private static bool LoadAssetPrefix(
            string originalAssetName,
            Type assetType,
            ref object __result,
            object __instance)
        {
            if (!Enabled)
                return true;

            try
            {
                if (!ContentPatchRegistry.TryGetReplacement(originalAssetName, out _, out var filePath))
                    return true;

                var replacement = ContentAssetLoader.TryLoadReplacement(__instance, assetType, filePath);
                if (replacement == null)
                    return true;

                __result = replacement;
                return false;
            }
            catch (Exception ex)
            {
                AsherLog.Error($"[ContentPatcher] Failed to load replacement for '{originalAssetName}': {ex.Message}");
                return true;
            }
        }

        private static MethodInfo FindLoadAssetMethod(Type contentManagerType)
        {
            return contentManagerType
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault(method =>
                    method.Name == "LoadAsset"
                    && method.GetParameters().Length == 3
                    && method.GetParameters()[0].ParameterType == typeof(string)
                    && method.GetParameters()[1].ParameterType == typeof(Type)
                    && !method.IsGenericMethodDefinition);
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
