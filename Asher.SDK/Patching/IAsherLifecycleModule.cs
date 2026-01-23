namespace Asher.SDK.Patching
{
    public interface IAsherLifecycleModule
    {
        string Name { get; }

        void OnGameInitialized(); /// Chamado após Game1.Initialize
        void OnContentLoaded(); /// Chamado após Game1.LoadContent
        void OnGamePaused(); /// Chamado quando o jogo está pausando
        void OnGameExiting(); /// Chamado quando o jogo está saindo
    }
}