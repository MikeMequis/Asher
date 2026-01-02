using System.Diagnostics;

namespace Asher.Services.Interfaces
{
    public interface IDllInjectorService
    {
        bool InjectDll(Process process, string dllPath);
        bool CopyFilesToGameFolder(string gameFolder, string[] sourceFiles, string[] targetNames);
    }
}

