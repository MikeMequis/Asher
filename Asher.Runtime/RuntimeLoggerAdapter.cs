using Asher.SDK.Logging;

namespace Asher.Runtime.Logging
{
    internal sealed class RuntimeLoggerAdapter : IAsherLogger
    {
        public void Info(string message) => RuntimeLogger.Info(message);
        public void Warning(string message) => RuntimeLogger.Warning(message);
        public void Error(string message) => RuntimeLogger.Error(message);
    }
}
