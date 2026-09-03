using System;

namespace Asher.SDK.Patching
{
    /// <summary>
    /// Assembly-level mod metadata for the Patch Manager UI.
    /// Apply once per mod DLL: [assembly: AsherMod("Display Name", "Description")].
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
    public sealed class AsherModAttribute : Attribute
    {
        public AsherModAttribute(string displayName, string description = "")
        {
            DisplayName = displayName ?? string.Empty;
            Description = description ?? string.Empty;
        }

        /// <summary>
        /// User-facing name shown in the Patch Manager.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Short description shown under the mod name.
        /// </summary>
        public string Description { get; }
    }
}
