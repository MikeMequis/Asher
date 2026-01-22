using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;

namespace Asher.Runtime.Bootstrap
{
    public static class HarmonyBootstrap
    {
        private static bool _initialized;

        public static void Initialize()
        {
            if (_initialized)
            {
                RuntimeLogger.Warning("Harmony já inicializado.");
                return;
            }

            try
            {
                RuntimeLogger.Info("Criando instância do Harmony...");
                var harmony = new Harmony("com.asher.runtime");
                RuntimeLogger.Info("Harmony criado com sucesso.");

                RuntimeLogger.Info("Aplicando lifecycle hook...");
                ApplyLifecycleHook(harmony);

                _initialized = true;
                RuntimeLogger.Info("Harmony inicializado (lifecycle hook aplicado).");
            }
            catch (Exception ex)
            {
                RuntimeLogger.Fatal("Falha ao inicializar Harmony", ex);
                throw;
            }
        }

        private static void ApplyLifecycleHook(Harmony harmony)
        {
            RuntimeLogger.Info("Procurando assembly DustAET...");

            // NÃO use Type.GetType() - use o assembly diretamente
            var dustAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "DustAET");

            if (dustAssembly == null)
            {
                RuntimeLogger.Error("Assembly DustAET não encontrado.");
                return;
            }

            RuntimeLogger.Info($"Assembly DustAET encontrado: {dustAssembly.FullName}");
            RuntimeLogger.Info("Procurando tipo Dust.Game1...");

            var game1Type = dustAssembly.GetType("Dust.Game1");

            if (game1Type == null)
            {
                RuntimeLogger.Error("Tipo Dust.Game1 não encontrado.");

                // Lista todos os tipos para debug
                RuntimeLogger.Info("Tipos disponíveis no assembly DustAET:");
                var types = dustAssembly.GetTypes()
                    .Where(t => t.Namespace == "Dust")
                    .Take(20);

                foreach (var t in types)
                {
                    RuntimeLogger.Info($"  - {t.FullName}");
                }

                return;
            }

            RuntimeLogger.Info($"Tipo Dust.Game1 encontrado: {game1Type.FullName}");
            RuntimeLogger.Info("Procurando método Initialize...");

            var initializeMethod = game1Type.GetMethod(
                "Initialize",
                BindingFlags.Instance |
                BindingFlags.NonPublic |
                BindingFlags.Public
            );

            if (initializeMethod == null)
            {
                RuntimeLogger.Error("Método Game1.Initialize não encontrado.");

                // Lista todos os métodos disponíveis
                RuntimeLogger.Info("Métodos disponíveis em Game1:");
                var methods = game1Type.GetMethods(
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic
                ).Take(30);

                foreach (var method in methods)
                {
                    RuntimeLogger.Info($"  - {method.Name} ({method.GetParameters().Length} params)");
                }

                return;
            }

            RuntimeLogger.Info($"Método Initialize encontrado: {initializeMethod.Name}");
            RuntimeLogger.Info("Aplicando patch...");

            try
            {
                harmony.Patch(
                    original: initializeMethod,
                    postfix: new HarmonyMethod(
                        typeof(GameInitHook),
                        nameof(GameInitHook.Postfix)
                    )
                );

                RuntimeLogger.Info("✓ Hook em Game1.Initialize aplicado com sucesso!");
            }
            catch (Exception ex)
            {
                RuntimeLogger.Error("Falha ao aplicar patch do Harmony", ex);
                throw;
            }
        }
    }
}