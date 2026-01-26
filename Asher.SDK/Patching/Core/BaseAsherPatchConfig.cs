using Asher.SDK.Logging;
using System;
using System.Reflection;

namespace Asher.SDK.Patching.Core
{
    /// <summary>
    /// Classe base para configuração de patches via PreInit.
    /// </summary>
    /// <typeparam name="TPatch">Tipo do patch que será configurado (deve ter propriedade estática Enabled)</typeparam>
    public abstract class BaseAsherPatchConfig<TPatch> : IAsherPreInitModule where TPatch : class
    {
        /// <summary>
        /// Nome do patch (usado em logs).
        /// </summary>
        protected abstract string PatchName { get; }

        /// <summary>
        /// Tag de log (usado como prefixo nos logs).
        /// </summary>
        protected virtual string LogTag => $"[{PatchName.Replace(" ", "")}]";

        /// <summary>
        /// Nome do módulo PreInit (exibido no log de carregamento).
        /// </summary>
        public string Name => $"{PatchName} Config";

        /// <summary>
        /// Determina se o patch deve ser habilitado.
        /// </summary>
        /// <returns>true para habilitar o patch, false para desabilitar</returns>
        protected virtual bool ShouldEnable()
        {
            // Por padrão, sempre habilita
            return true;
        }

        /// <summary>
        /// Mensagem de log quando o patch é habilitado.
        /// </summary>
        protected virtual string EnabledMessage => $"{PatchName} será habilitado";

        /// <summary>
        /// Mensagem de log quando o patch é desabilitado.
        /// </summary>
        protected virtual string DisabledMessage => $"{PatchName} está desabilitado";

        /// <summary>
        /// Executa a configuração do patch.
        /// </summary>
        public void Execute()
        {
            try
            {
                bool shouldEnable = ShouldEnable();

                // Usa reflection para definir a propriedade Enabled estática
                var enabledProperty = typeof(TPatch).GetProperty("Enabled",
                    BindingFlags.Public | BindingFlags.Static);

                if (enabledProperty == null)
                {
                    AsherLog.Error($"{LogTag} Propriedade 'Enabled' não encontrada em {typeof(TPatch).Name}");
                    return;
                }

                enabledProperty.SetValue(null, shouldEnable);

                // Log apropriado
                if (shouldEnable)
                {
                    AsherLog.Info($"{LogTag} {EnabledMessage}");
                    OnEnabled();
                }
                else
                {
                    AsherLog.Info($"{LogTag} {DisabledMessage}");
                    OnDisabled();
                }
            }
            catch (Exception ex)
            {
                AsherLog.Error($"{LogTag} Erro ao configurar patch: {ex.Message}");
            }
        }

        /// <summary>
        /// Chamado quando o patch é habilitado.
        /// </summary>
        protected virtual void OnEnabled() { }

        /// <summary>
        /// Chamado quando o patch é desabilitado.
        /// </summary>
        protected virtual void OnDisabled() { }
    }
}