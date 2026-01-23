namespace Asher.SDK.Patching
{
    public interface IAsherLifecycleModule
    {
        string Name { get; }

        /// <summary>
        /// Chamado após Game1.Initialize
        /// </summary>
        void OnGameInitialized();

        /// <summary>
        /// Chamado após Game1.LoadContent
        /// </summary>
        void OnContentLoaded();

        /// <summary>
        /// Chamado quando o jogo está pausando
        /// </summary>
        void OnGamePaused();

        /// <summary>
        /// Chamado quando o jogo está saindo
        /// </summary>
        void OnGameExiting();
    }

    public abstract class AsherLifecycleModuleBase : IAsherLifecycleModule
    {
        public abstract string Name { get; }

        public virtual void OnGameInitialized() { }
        public virtual void OnContentLoaded() { }
        public virtual void OnGamePaused() { }
        public virtual void OnGameExiting() { }
    }
}