using System;
using System.Reflection;

namespace Asher.SDK.Patching
{
    /// <summary>
    /// Reads display metadata from [assembly: AsherMod] on the declaring assembly.
    /// </summary>
    public static class AsherModMetadata
    {
        public static string GetDisplayName(Type type) => GetDisplayName(type.Assembly);

        public static string GetDisplayName(Assembly assembly)
        {
            var mod = assembly.GetCustomAttribute<AsherModAttribute>();
            if (mod != null && !string.IsNullOrWhiteSpace(mod.DisplayName))
                return mod.DisplayName;

            var title = assembly.GetCustomAttribute<AssemblyTitleAttribute>();
            if (title != null && !string.IsNullOrWhiteSpace(title.Title))
                return title.Title;

            return assembly.GetName().Name ?? "Unknown mod";
        }
    }
}
