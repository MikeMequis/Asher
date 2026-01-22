namespace Asher.SDK.Logging
{
    public interface IAsherLogger
    {
        void Info(string message);
        void Warning(string message);
        void Error(string message);
    }
}