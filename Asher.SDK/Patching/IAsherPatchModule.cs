using HarmonyLib;

namespace Asher.SDK.Patching
{
    /// <summary>
    /// Interface para módulos de patch que utilizam Harmony.
    /// Implementações devem aplicar patches durante a fase de carregamento.
    /// </summary>
    public interface IAsherPatchModule
    {
        /// <summary>
        /// Nome do módulo de patch.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Aplica os patches usando a instância Harmony fornecida.
        /// </summary>
        /// <param name="harmony">Instância Harmony para aplicar patches</param>
        void Apply(Harmony harmony);
    }
}
