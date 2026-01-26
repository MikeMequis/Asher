namespace Asher.SDK.Patching
{
    /// <summary>
    /// Interface para módulos que reagem a eventos do ciclo de vida do jogo.
    /// </summary>
    public interface IAsherLifecycleModule
    {
        /// <summary>
        /// Nome do módulo de lifecycle.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Chamado após Game1.Initialize() completar.
        /// ✅ IMPLEMENTADO E FUNCIONANDO
        /// </summary>
        void OnGameInitialized();

        /// <summary>
        /// Chamado após Game1.LoadContent() completar.
        /// ✅ IMPLEMENTADO E FUNCIONANDO
        /// </summary>
        void OnContentLoaded();

        /// <summary>
        /// Chamado quando o jogo é pausado.
        /// ⚠️ NÃO IMPLEMENTADO - Requer hook em Game1.OnDeactivated() ou menu de pausa
        /// </summary>
        void OnGamePaused();

        /// <summary>
        /// Chamado quando o jogo está finalizando.
        /// ⚠️ NÃO IMPLEMENTADO - Requer hook em Game1.OnExiting()
        /// </summary>
        void OnGameExiting();
    }
}