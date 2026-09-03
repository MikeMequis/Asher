namespace Asher.SDK.Patching.Core
{
    /// <summary>
    /// Base PreInit module. Name comes from [assembly: AsherMod] on the mod DLL.
    /// </summary>
    public abstract class BaseAsherPreInitModule : IAsherPreInitModule
    {
        public virtual string Name => AsherModMetadata.GetDisplayName(GetType());

        public abstract void Execute();
    }
}
