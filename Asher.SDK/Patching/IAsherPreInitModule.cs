namespace Asher.SDK.Patching
{
    public interface IAsherPreInitModule
    {
        string Name { get; }
        void Execute();
    }
}