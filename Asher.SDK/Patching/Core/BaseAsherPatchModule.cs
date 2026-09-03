using HarmonyLib;
using System;
using System.Collections.Generic;

namespace Asher.SDK.Patching.Core
{
    /// <summary>
    /// Base Harmony patch module. Name comes from [assembly: AsherMod] on the mod DLL.
    /// </summary>
    public abstract class BaseAsherPatchModule : IAsherPatchModule
    {
        public virtual string Name => AsherModMetadata.GetDisplayName(GetType());

        public abstract void Apply(Harmony harmony);

        public virtual IEnumerable<Type> GetPatchTypes() => Array.Empty<Type>();
    }
}
