namespace Asher.Runtime.Lifecycle
{
    public enum LifecycleEvent
    {
        GameInitialized, /// Disparado após Game1.Initialize() completar
        ContentLoaded, /// Disparado após Game1.LoadContent() completar
        GamePaused,  /// Disparado quando o jogo é pausado
        GameResumed, /// Disparado quando o jogo volta de pausa
        GameExiting /// Disparado quando o jogo está finalizando
    }
}