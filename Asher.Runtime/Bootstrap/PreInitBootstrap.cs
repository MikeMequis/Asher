using Asher.SDK.Patching;
using System;
using System.Linq;
using System.Reflection;

namespace Asher.Runtime.Bootstrap
{
    public static class PreInitBootstrap
    {
        public static void ExecutePreInitModules()
        {
            RuntimeLogger.Info("[PreInit] Iniciando busca por módulos PreInit...");

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            RuntimeLogger.Info($"[PreInit] Total de assemblies carregados: {assemblies.Length}");

            int modulesFound = 0;
            int modulesExecuted = 0;

            foreach (var asm in assemblies)
            {
                RuntimeLogger.Info($"[PreInit] Verificando assembly: {asm.GetName().Name}");

                Type[] types;

                try
                {
                    types = asm.GetTypes();
                    RuntimeLogger.Info($"[PreInit] {types.Length} tipos encontrados em {asm.GetName().Name}");
                }
                catch (ReflectionTypeLoadException e)
                {
                    RuntimeLogger.Warning($"[PreInit] ReflectionTypeLoadException em {asm.GetName().Name}");
                    types = e.Types.Where(t => t != null).ToArray()!;

                    // Log loader exceptions
                    foreach (var ex in e.LoaderExceptions)
                    {
                        if (ex != null)
                            RuntimeLogger.Warning($"[PreInit] Loader Exception: {ex.Message}");
                    }
                }
                catch (Exception ex)
                {
                    RuntimeLogger.Error($"[PreInit] Erro ao obter tipos de {asm.GetName().Name}: {ex.Message}");
                    continue;
                }

                foreach (var type in types)
                {
                    try
                    {
                        if (type == null)
                            continue;

                        bool isPreInitModule = typeof(IAsherPreInitModule).IsAssignableFrom(type);
                        bool isAbstract = type.IsAbstract;
                        bool isInterface = type.IsInterface;

                        if (isPreInitModule && !isAbstract && !isInterface)
                        {
                            modulesFound++;
                            RuntimeLogger.Info($"[PreInit] Módulo encontrado: {type.FullName}");

                            try
                            {
                                var module = (IAsherPreInitModule)Activator.CreateInstance(type)!;
                                RuntimeLogger.Info($"[PreInit] Executando módulo: {module.Name}");

                                module.Execute();

                                modulesExecuted++;
                                RuntimeLogger.Info($"[PreInit] Módulo executado com sucesso: {module.Name}");
                            }
                            catch (Exception ex)
                            {
                                RuntimeLogger.Error(
                                    $"[PreInit] Falha ao executar módulo {type.FullName}: {ex.Message}",
                                    ex
                                );
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        RuntimeLogger.Error($"[PreInit] Erro ao processar tipo: {ex.Message}");
                    }
                }
            }

            RuntimeLogger.Info($"[PreInit] Resumo: {modulesFound} módulos encontrados, {modulesExecuted} executados com sucesso.");
            RuntimeLogger.Info("[PreInit] Busca por módulos PreInit concluída.");
        }
    }
}