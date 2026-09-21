using System.Runtime.InteropServices;

namespace Asher.Core.Platform
{
    /// <summary>
    /// Resolves the platform descriptor for the current process.
    /// </summary>
    public static class PlatformInfo
    {
        private static readonly IPlatformInfo CurrentInstance = Create();

        public static IPlatformInfo Current => CurrentInstance;

        private static IPlatformInfo Create()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return new WindowsPlatformInfo();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return new LinuxPlatformInfo();

            // Unsupported OS (e.g. macOS): keep the established Windows behavior.
            return new WindowsPlatformInfo();
        }
    }
}
