using Asher.Runtime.Logging;
using System.IO;

namespace Asher.Runtime.Core
{
    public sealed class RuntimeController
    {
        private bool _initialized;

        public void Init(RuntimeContext context)
        {
            if (_initialized)
                return;

            _initialized = true;

            RuntimeLogger.Init(context.LogPath);
            RuntimeLogger.Info("Runtime iniciado");

            Validate(context);
            PrepareDirectories(context);

            RuntimeLogger.Info("Runtime pronto (pré-jogo)");
        }

        private void Validate(RuntimeContext context)
        {
            if (!Directory.Exists(context.GamePath))
                throw new DirectoryNotFoundException("GamePath inválido");

            RuntimeLogger.Info($"GamePath: {context.GamePath}");
            RuntimeLogger.Info($"Profile: {context.ProfileName}");
        }

        private void PrepareDirectories(RuntimeContext context)
        {
            if (!Directory.Exists(context.ModsPath))
            {
                Directory.CreateDirectory(context.ModsPath);
                RuntimeLogger.Info("Diretório de mods criado");
            }
        }
    }
}
