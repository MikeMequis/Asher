using Asher.SDK.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Asher.Patching.Debug
{
    public sealed class DebugPatchModule : IAsherPatchModule
    {
        public string Name => "Debug Enabler";

        public void Apply(Harmony harmony)
        {
            if (!DebugState.EnableDebug)
            {
                AsherLog.Info("[DebugPatch] Debug desabilitado, patch não será aplicado.");
                return;
            }

            AsherLog.Info("[DebugPatch] Iniciando aplicação do patch...");

            try
            {
                // Busca o assembly DustAET
                var dustAssembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "DustAET");

                if (dustAssembly == null)
                {
                    AsherLog.Error("[DebugPatch] Assembly DustAET não encontrado!");
                    return;
                }

                // Busca o tipo Game1
                var game1Type = dustAssembly.GetType("Dust.Game1");

                if (game1Type == null)
                {
                    AsherLog.Error("[DebugPatch] Tipo Dust.Game1 não encontrado!");
                    return;
                }

                // Busca o método Initialize
                var initMethod = game1Type.GetMethod(
                    "Initialize",
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic
                );

                if (initMethod == null)
                {
                    AsherLog.Error("[DebugPatch] Método Initialize não encontrado!");
                    return;
                }

                AsherLog.Info($"[DebugPatch] Aplicando patch em {game1Type.FullName}.Initialize...");

                harmony.Patch(
                    initMethod,
                    postfix: new HarmonyMethod(
                        typeof(DebugPatchModule),
                        nameof(EnableDebug)
                    )
                );

                AsherLog.Info("[DebugPatch] ✓ Patch aplicado com sucesso!");
            }
            catch (Exception ex)
            {
                AsherLog.Error($"[DebugPatch] Erro ao aplicar patch: {ex.Message}");
            }
        }

        static void EnableDebug(object __instance)
        {
            try
            {
                AsherLog.Info("[DebugPatch] EnableDebug chamado!");

                var game1Type = __instance.GetType();
                var canDebugField = game1Type.GetField(
                    "canDebug",
                    System.Reflection.BindingFlags.Static |
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic
                );

                if (canDebugField != null)
                {
                    canDebugField.SetValue(null, true);
                    AsherLog.Info("[DebugPatch] ✓ canDebug = true");
                }
                else
                {
                    AsherLog.Warning("[DebugPatch] Campo canDebug não encontrado!");

                    // Lista campos disponíveis
                    AsherLog.Info("[DebugPatch] Campos estáticos disponíveis:");
                    foreach (var field in game1Type.GetFields(
                        System.Reflection.BindingFlags.Static |
                        System.Reflection.BindingFlags.Public |
                        System.Reflection.BindingFlags.NonPublic))
                    {
                        AsherLog.Info($"  - {field.Name} ({field.FieldType.Name})");
                    }
                }
            }
            catch (Exception ex)
            {
                AsherLog.Error($"[DebugPatch] Erro em EnableDebug: {ex.Message}");
            }
        }

        public IEnumerable<Type> GetPatchTypes()
            => Array.Empty<Type>();
    }
}