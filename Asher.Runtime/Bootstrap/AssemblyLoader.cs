using System;
using System.IO;
using System.Reflection;

namespace Asher.Runtime.Bootstrap
{
    public static class AssemblyLoader
    {
        public static void LoadAssembliesFrom(string directory)
        {
            RuntimeLogger.Info($"[AssemblyLoader] Verificando diretório: {directory}");

            if (!Directory.Exists(directory))
            {
                RuntimeLogger.Warning($"[AssemblyLoader] Diretório não existe: {directory}");
                return;
            }

            var dllFiles = Directory.GetFiles(directory, "*.dll");
            RuntimeLogger.Info($"[AssemblyLoader] {dllFiles.Length} DLLs encontradas.");

            foreach (var dll in dllFiles)
            {
                var fileName = Path.GetFileName(dll);
                RuntimeLogger.Info($"[AssemblyLoader] Tentando carregar: {fileName}");

                try
                {
                    var asm = Assembly.LoadFrom(dll);
                    RuntimeLogger.Info($"[AssemblyLoader] ✓ Assembly carregado: {fileName} ({asm.FullName})");
                }
                catch (ReflectionTypeLoadException ex)
                {
                    RuntimeLogger.Error($"[AssemblyLoader] Falha ao carregar tipos de {fileName}");

                    if (ex.LoaderExceptions != null)
                    {
                        foreach (var loaderEx in ex.LoaderExceptions)
                        {
                            if (loaderEx != null)
                                RuntimeLogger.Error($"  - {loaderEx.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    RuntimeLogger.Warning($"[AssemblyLoader] Falha ao carregar {fileName}: {ex.Message}");
                }
            }

            RuntimeLogger.Info("[AssemblyLoader] Carregamento concluído.");
        }
    }
}