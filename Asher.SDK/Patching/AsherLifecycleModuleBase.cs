namespace Asher.SDK.Patching
{
    public abstract class AsherLifecycleModuleBase : IAsherLifecycleModule
    {
        public abstract string Name { get; }

        public virtual void OnGameInitialized() { } /// ✅ Hook aplicado: Game1.Initialize (postfix)
        public virtual void OnContentLoaded() { } /// ✅ Hook aplicado: Game1.LoadContent (postfix)
        public virtual void OnGamePaused() { } /// ⚠️ Hook NÃO aplicado ainda - evento não será disparado
        public virtual void OnGameExiting() { } /// ⚠️ Hook NÃO aplicado ainda - evento não será disparado
    }
}