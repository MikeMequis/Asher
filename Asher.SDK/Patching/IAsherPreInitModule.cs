namespace Asher.SDK.Patching
{
    /// <summary>
    /// Interface para módulos PreInit que executam antes da aplicação de patches.
    /// Usado para configuração inicial, definir flags, ou preparar estado.
    /// </summary>
    public interface IAsherPreInitModule
    {
        /// <summary>
        /// Nome do módulo PreInit.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Executa a lógica de pré-inicialização.
        /// Chamado antes de qualquer patch ser aplicado.
        /// </summary>
        void Execute();
    }
}