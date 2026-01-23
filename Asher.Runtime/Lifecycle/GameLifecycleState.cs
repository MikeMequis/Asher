namespace Asher.Runtime.Lifecycle
{
    public enum GameLifecycleState
    {
        /// <summary>
        /// Estado inicial, antes de qualquer inicialização
        /// </summary>
        None,

        /// <summary>
        /// Runtime inicializando
        /// </summary>
        RuntimeInitializing,

        /// <summary>
        /// Mods sendo carregados
        /// </summary>
        LoadingMods,

        /// <summary>
        /// Patches sendo aplicados
        /// </summary>
        ApplyingPatches,

        /// <summary>
        /// Jogo inicializando (Game1.Initialize)
        /// </summary>
        GameInitializing,

        /// <summary>
        /// Jogo inicializado
        /// </summary>
        GameInitialized,

        /// <summary>
        /// Conteúdo sendo carregado (Game1.LoadContent)
        /// </summary>
        LoadingContent,

        /// <summary>
        /// Conteúdo carregado
        /// </summary>
        ContentLoaded,

        /// <summary>
        /// Jogo em execução (loop principal)
        /// </summary>
        Running,

        /// <summary>
        /// Jogo pausado
        /// </summary>
        Paused,

        /// <summary>
        /// Jogo finalizando
        /// </summary>
        Exiting
    }

    public enum LifecycleEvent
    {
        GameInitialized,
        ContentLoaded,
        GameStarted,
        GamePaused,
        GameResumed,
        GameExiting
    }
}