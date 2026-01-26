using Asher.SDK.Logging;
using System.Reflection;

namespace Asher.SDK.Patching.Core
{
    /// <summary>
    /// Classe base para módulos de lifecycle com comportamento padrão configurável.
    /// </summary>
    public abstract class BaseAsherLifecycle : IAsherLifecycleModule
    {
        /// <summary>
        /// Nome do módulo (usado em logs).
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Tag de log (padrão: nome do módulo entre colchetes).
        /// </summary>
        protected virtual string LogTag => $"[{Name}]";

        /// <summary>
        /// Se true, loga automaticamente quando eventos são disparados.
        /// Se false, apenas executa a lógica customizada sem log automático.
        /// </summary>
        protected virtual bool EnableAutoLogging => false;

        /// <summary>
        /// Chamado após Game1.Initialize() completar.
        /// </summary>
        public virtual void OnGameInitialized()
        {
            if (EnableAutoLogging)
                AsherLog.Info($"{LogTag} Game initialized");
        }

        /// <summary>
        /// Chamado após Game1.LoadContent() completar.
        /// </summary>
        public virtual void OnContentLoaded()
        {
            if (EnableAutoLogging)
                AsherLog.Info($"{LogTag} Content loaded");
        }

        /// <summary>
        /// Chamado quando o jogo é pausado.
        /// ⚠️ Hook ainda não implementado no runtime.
        /// </summary>
        public virtual void OnGamePaused()
        {
            if (EnableAutoLogging)
                AsherLog.Info($"{LogTag} Game paused");
        }

        /// <summary>
        /// Chamado quando o jogo está finalizando.
        /// ⚠️ Hook ainda não implementado no runtime.
        /// </summary>
        public virtual void OnGameExiting()
        {
            if (EnableAutoLogging)
                AsherLog.Info($"{LogTag} Game exiting");
        }
    }

    /// <summary>
    /// Classe base para lifecycles que monitoram patches condicionais.
    /// Fornece helpers para verificar se um patch está ativo.
    /// </summary>
    /// <typeparam name="TPatch">Tipo do patch associado (deve ter propriedade Enabled)</typeparam>
    public abstract class BaseAsherPatchLifecycle<TPatch> : BaseAsherLifecycle where TPatch : class
    {
        /// <summary>
        /// Verifica se o patch associado está habilitado.
        /// </summary>
        protected bool IsPatchEnabled
        {
            get
            {
                var enabledProperty = typeof(TPatch).GetProperty("Enabled",
                    BindingFlags.Public | BindingFlags.Static);

                return enabledProperty != null && (bool)enabledProperty.GetValue(null)!;
            }
        }

        /// <summary>
        /// Executa uma ação apenas se o patch estiver habilitado.
        /// Útil para evitar verificações repetitivas.
        /// </summary>
        protected void ExecuteIfEnabled(System.Action action, string? notEnabledMessage = null)
        {
            if (!IsPatchEnabled)
            {
                if (notEnabledMessage != null)
                    AsherLog.Info($"{LogTag} {notEnabledMessage}");
                return;
            }

            try
            {
                action();
            }
            catch (System.Exception ex)
            {
                AsherLog.Error($"{LogTag} Erro: {ex.Message}");
            }
        }
    }
}