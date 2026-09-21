using Asher.SDK.Logging;
using Asher.SDK.Patching.Core;
using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Asher.Patching.DebugEnabler
{
    /// <summary>
    /// Habilita o menu de debug do jogo (Tab no menu de pausa).
    /// </summary>
    public sealed class DebugEnablerPatch : BaseAsherPatchModule
    {
        private const BindingFlags TargetFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static int _postAttachApplied;

        public static bool Enabled { get; set; }

        public override void Apply(Harmony harmony)
        {
            if (!Enabled)
            {
                AsherLog.Info("[DebugEnabler] Patch desabilitado por configuração");
                return;
            }

            try
            {
                var game1Type = AppDomain.CurrentDomain
                    .GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "DustAET")
                    ?.GetType("Dust.Game1");

                var initMethod = ResolveDeclaredMethod(game1Type, "Initialize");

                if (initMethod == null)
                {
                    AsherLog.Warning("[DebugEnabler] Não foi possível encontrar Game1.Initialize");
                    return;
                }

                harmony.Patch(initMethod,
                    postfix: new HarmonyMethod(typeof(DebugEnablerPatch), nameof(EnableDebugMenu)));

                AsherLog.Info($"[DebugEnabler] Patched {initMethod.DeclaringType?.FullName}.{initMethod.Name}");

                // Late-attach fallback (Linux LD_PRELOAD): if the game already ran Initialize before
                // the runtime attached, the postfix above will never fire. Run the same callback
                // from the next frame instead. Idempotent.
                var tickMethod = ResolveDeclaredMethod(game1Type, "Tick");
                if (tickMethod != null)
                {
                    harmony.Patch(tickMethod,
                        prefix: new HarmonyMethod(typeof(DebugEnablerPatch), nameof(EnsureDebugOnTick)));

                    AsherLog.Info(
                        $"[DebugEnabler] Post-attach hook: {tickMethod.DeclaringType?.FullName}.{tickMethod.Name}");
                }
            }
            catch (Exception ex)
            {
                AsherLog.Error($"[DebugEnabler] Erro: {ex.Message}");
            }
        }

        /// <summary>
        /// Resolves the method Harmony must patch. Reflection can return an inherited method
        /// (ReflectedType differs from DeclaringType) and Harmony only patches declared members,
        /// so in that case the method is re-resolved from its declaring type
        /// (e.g. Dust.Game1.Initialize -> Microsoft.Xna.Framework.Game.Initialize). When the type
        /// declares its own method the result is unchanged.
        /// </summary>
        public static MethodInfo ResolveDeclaredMethod(Type type, string methodName)
        {
            if (type == null)
                return null;

            var candidate = type.GetMethod(methodName, TargetFlags);
            if (candidate == null)
                return null;

            var declaringType = candidate.DeclaringType;
            if (declaringType == null || candidate.ReflectedType == declaringType)
                return candidate;

            var declared = declaringType.GetMethod(
                candidate.Name,
                TargetFlags | BindingFlags.DeclaredOnly);
            if (declared != null)
                return declared;

            var baseDefinition = candidate.GetBaseDefinition();
            if (baseDefinition != null && baseDefinition.DeclaringType != null)
            {
                declared = baseDefinition.DeclaringType.GetMethod(
                    baseDefinition.Name,
                    TargetFlags | BindingFlags.DeclaredOnly);
                if (declared != null)
                    return declared;
            }

            return candidate;
        }

        private static void EnsureDebugOnTick(object __instance)
        {
            if (Interlocked.CompareExchange(ref _postAttachApplied, 1, 0) != 0)
                return;

            EnableDebugMenu(__instance);
            AsherLog.Info("[DebugEnabler] canDebug enabled via post-attach Game.Tick");
        }

        private static void EnableDebugMenu(object __instance)
        {
            __instance.GetType()
                .GetField("canDebug", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                ?.SetValue(null, true);
        }
    }
}
