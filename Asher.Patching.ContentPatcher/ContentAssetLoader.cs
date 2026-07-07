using Asher.SDK.Logging;
using System;
using System.IO;
using System.Reflection;

namespace Asher.Patching.ContentPatcher
{
    internal static class ContentAssetLoader
    {
        public static object TryLoadReplacement(object contentManager, Type assetType, string filePath)
        {
            if (assetType == null)
                return null;

            if (assetType.FullName == "Microsoft.Xna.Framework.Graphics.Texture2D")
                return LoadTexture(contentManager, filePath);

            AsherLog.Warning($"[ContentPatcher] Unsupported replacement type {assetType.Name} for {filePath}");
            return null;
        }

        private static object LoadTexture(object contentManager, string filePath)
        {
            var graphicsDevice = GetGraphicsDevice(contentManager);
            if (graphicsDevice == null)
            {
                AsherLog.Warning("[ContentPatcher] GraphicsDevice unavailable for texture replacement");
                return null;
            }

            var textureType = graphicsDevice.GetType().Assembly
                .GetType("Microsoft.Xna.Framework.Graphics.Texture2D");

            var fromStreamMethod = textureType?.GetMethod(
                "FromStream",
                BindingFlags.Static | BindingFlags.Public,
                null,
                new[] { graphicsDevice.GetType(), typeof(Stream) },
                null);

            if (fromStreamMethod == null)
            {
                AsherLog.Warning("[ContentPatcher] Texture2D.FromStream not found");
                return null;
            }

            using (var stream = File.OpenRead(filePath))
            {
                return fromStreamMethod.Invoke(null, new[] { graphicsDevice, stream });
            }
        }

        private static object GetGraphicsDevice(object contentManager)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            var managerType = contentManager.GetType();

            var graphicsDeviceProperty = managerType.GetProperty("GraphicsDevice", flags);
            if (graphicsDeviceProperty != null)
            {
                var device = graphicsDeviceProperty.GetValue(contentManager);
                if (device != null)
                    return device;
            }

            var serviceProviderField = managerType.GetField("serviceProvider", flags)
                ?? managerType.GetField("_serviceProvider", flags);

            if (serviceProviderField == null)
                return null;

            var serviceProvider = serviceProviderField.GetValue(contentManager) as IServiceProvider;
            if (serviceProvider == null)
                return null;

            var graphicsDeviceType = managerType.Assembly
                .GetType("Microsoft.Xna.Framework.Graphics.GraphicsDevice");

            return serviceProvider.GetService(graphicsDeviceType);
        }
    }
}
