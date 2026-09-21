using Asher.Patching.DebugEnabler;
using Asher.Runtime.Diagnostics;
using HarmonyLib;
using System;
using System.Reflection;

namespace Asher.HarmonyPoc
{
    /// <summary>
    /// Linux PoC adapter for the real Asher DebugEnabler patch.
    ///
    /// The real patch is Patches/Asher.Patching.DebugEnabler/DebugEnablerPatch.cs, used unchanged.
    /// Its <c>Apply(Harmony)</c> cannot run as-is here because it resolves the target with
    /// <c>game1Type.GetMethod("Initialize", ...)</c>, which returns the inherited
    /// Microsoft.Xna.Framework.Game.Initialize; Harmony refuses inherited MethodInfos.
    ///
    /// Linux lifecycle finding: this is an LD_PRELOAD/late attach. The native bootstrap cannot
    /// safely attach before the embedded Mono runtime is fully initialised (attaching too early
    /// aborts in mono_thread_attach), and by the time it does attach FNA has already run its
    /// one-shot Game.Initialize. A postfix on Initialize therefore never executes, no matter how
    /// valid the patch is.
    ///
    /// Fix (PoC boundary): keep the real DebugEnabler callback and install it on Game.Tick as
    /// well. Tick is a non-virtual FNA method that runs every frame after initialisation, so the
    /// real <c>EnableDebugMenu</c> callback is guaranteed to execute. It only sets the static
    /// canDebug flag, which is idempotent.
    ///
    /// This assembly is the only place that references 0Harmony.
    /// </summary>
    internal static class DebugEnablerHarmonyPatch
    {
        private const string HarmonyId = "Asher.Linux.DebugEnabler";
        private const string TargetTypeName = "Dust.Game1";
        private const string TargetMethodName = "Initialize";
        private const string PostAttachMethodName = "Tick";
        private const string RealPostfixName = "EnableDebugMenu";
        private const string CanDebugFieldName = "canDebug";
        private const string HasInitializedFieldName = "hasInitialized";

        private const BindingFlags TargetFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static int _postAttachPrefixLogged;
        private static int _postAttachPostfixLogged;

        public static void Apply()
        {
            Console.WriteLine("[Asher] Initializing Harmony");

            Harmony harmony;
            try
            {
                harmony = new Harmony(HarmonyId);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Asher] Harmony initialization failed");
                Console.WriteLine($"[Asher] {ex}");
                throw;
            }

            Console.WriteLine("[Asher] Harmony instance created");

            var dustAssembly = DustAssemblyProbe.FindLoadedDustAssembly();
            if (dustAssembly == null)
            {
                Console.WriteLine("[Asher] DebugEnabler target type not found: Dust assembly not identified");
                throw new InvalidOperationException(
                    "Dust assembly not identified; cannot resolve the DebugEnabler target");
            }

            Console.WriteLine("[Asher] DebugEnabler target resolution started");

            var game1Type = dustAssembly.GetType(TargetTypeName);
            if (game1Type == null)
            {
                Console.WriteLine($"[Asher] DebugEnabler target type not found: {TargetTypeName}");
                throw new InvalidOperationException(
                    $"DebugEnabler target type not found: {TargetTypeName}");
            }

            Console.WriteLine($"[Asher] Target type: {game1Type.FullName}");

            var declaredOnGame1 = game1Type.GetMethod(
                TargetMethodName,
                TargetFlags | BindingFlags.DeclaredOnly);
            Console.WriteLine(
                $"[Asher] {game1Type.Name} declares {TargetMethodName}: {declaredOnGame1 != null}");

            var targetMethod = ResolveDeclaredTarget(game1Type);
            if (targetMethod == null)
            {
                Console.WriteLine(
                    $"[Asher] DebugEnabler target method not found: {TargetTypeName}.{TargetMethodName}");
                throw new InvalidOperationException(
                    $"DebugEnabler target method not found: {TargetTypeName}.{TargetMethodName}");
            }

            var baseDefinition = targetMethod.GetBaseDefinition();
            Console.WriteLine($"[Asher] Target method: {targetMethod.Name}");
            Console.WriteLine($"[Asher] Target declaring type: {Describe(targetMethod.DeclaringType)}");
            Console.WriteLine(
                $"[Asher] Target base definition: {Describe(baseDefinition.DeclaringType)}.{baseDefinition.Name}");
            Console.WriteLine(
                $"[Asher] Target method virtual: {targetMethod.IsVirtual}, " +
                $"body present: {targetMethod.GetMethodBody() != null}");

            var realPostfix = typeof(DebugEnablerPatch).GetMethod(
                RealPostfixName,
                BindingFlags.Static | BindingFlags.NonPublic);

            if (realPostfix == null)
            {
                Console.WriteLine(
                    $"[Asher] DebugEnabler postfix method not found: {nameof(DebugEnablerPatch)}.{RealPostfixName}");
                throw new InvalidOperationException(
                    $"DebugEnabler postfix method not found: {RealPostfixName}");
            }

            Console.WriteLine("[Asher] Applying existing DebugEnabler postfix");

            try
            {
                // Original target (will only fire if this attach happens to precede Initialize).
                harmony.Patch(
                    targetMethod,
                    prefix: new HarmonyMethod(
                        typeof(DebugEnablerHarmonyPatch),
                        nameof(OnTargetReached)));
                harmony.Patch(
                    targetMethod,
                    postfix: new HarmonyMethod(realPostfix));
                harmony.Patch(
                    targetMethod,
                    postfix: new HarmonyMethod(
                        typeof(DebugEnablerHarmonyPatch),
                        nameof(OnPostfixReached)));
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Asher] DebugEnabler patch application failed");
                Console.WriteLine($"[Asher] {ex}");
                throw;
            }

            var patches = Harmony.GetPatchInfo(targetMethod);
            if (patches == null || patches.Postfixes == null || patches.Postfixes.Count == 0)
            {
                Console.WriteLine("[Asher] DebugEnabler patch application failed: postfix not installed");
                throw new InvalidOperationException(
                    $"DebugEnabler postfix was not installed on " +
                    $"{Describe(targetMethod.DeclaringType)}.{targetMethod.Name}");
            }

            Console.WriteLine(
                $"[Asher] DebugEnabler patch applied successfully " +
                $"(prefixes={patches.Prefixes.Count}, postfixes={patches.Postfixes.Count})");

            // Late-attach lifecycle fix: run the same real callback from a per-frame method that is
            // guaranteed to execute after this late attach.
            InstallPostAttachPatch(harmony, targetMethod.DeclaringType, realPostfix);
        }

        /// <summary>
        /// Installs the real DebugEnabler callback on FNA's Game.Tick (non-virtual, called every
        /// frame). This is required because Game.Initialize has already run by the time an
        /// LD_PRELOAD attach can safely reach the managed runtime.
        /// </summary>
        private static void InstallPostAttachPatch(Harmony harmony, Type gameType, MethodInfo realPostfix)
        {
            var tick = gameType.GetMethod(
                PostAttachMethodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.DeclaredOnly);

            if (tick == null)
            {
                Console.WriteLine(
                    $"[Asher] Post-attach target not found: {Describe(gameType)}.{PostAttachMethodName}");
                return;
            }

            Console.WriteLine(
                $"[Asher] Post-attach target: {Describe(tick.DeclaringType)}.{tick.Name}");

            harmony.Patch(
                tick,
                prefix: new HarmonyMethod(
                    typeof(DebugEnablerHarmonyPatch),
                    nameof(PostAttachPrefix)));
            harmony.Patch(
                tick,
                postfix: new HarmonyMethod(realPostfix));
            harmony.Patch(
                tick,
                postfix: new HarmonyMethod(
                    typeof(DebugEnablerHarmonyPatch),
                    nameof(PostAttachPostfix)));

            var tickPatches = Harmony.GetPatchInfo(tick);
            if (tickPatches == null || tickPatches.Postfixes == null || tickPatches.Postfixes.Count == 0)
            {
                Console.WriteLine("[Asher] Post-attach DebugEnabler patch application failed");
                return;
            }

            Console.WriteLine(
                $"[Asher] Post-attach DebugEnabler patch applied successfully " +
                $"(prefixes={tickPatches.Prefixes.Count}, postfixes={tickPatches.Postfixes.Count})");
        }

        /// <summary>
        /// Linux-specific target resolution. The real patch's Apply uses
        /// <c>GetMethod("Initialize", ...)</c>, which returns the inherited
        /// Microsoft.Xna.Framework.Game.Initialize. Harmony only patches declared members, so the
        /// declared method is resolved here. When Game1 declares its own Initialize this is a no-op.
        /// </summary>
        private static MethodInfo ResolveDeclaredTarget(Type game1Type)
        {
            var candidate = game1Type.GetMethod(TargetMethodName, TargetFlags);
            if (candidate == null)
            {
                return null;
            }

            var declaringType = candidate.DeclaringType;
            if (declaringType == null || candidate.ReflectedType == declaringType)
            {
                return candidate;
            }

            var declared = declaringType.GetMethod(
                candidate.Name,
                TargetFlags | BindingFlags.DeclaredOnly);
            if (declared != null)
            {
                return declared;
            }

            var baseDefinition = candidate.GetBaseDefinition();
            if (baseDefinition != null && baseDefinition.DeclaringType != null)
            {
                declared = baseDefinition.DeclaringType.GetMethod(
                    baseDefinition.Name,
                    TargetFlags | BindingFlags.DeclaredOnly);
                if (declared != null)
                {
                    return declared;
                }
            }

            return candidate;
        }

        private static void OnTargetReached()
        {
            Console.WriteLine("[Asher] DebugEnabler target method reached");
        }

        private static void OnPostfixReached(object __instance)
        {
            var field = GetStaticField(__instance?.GetType(), CanDebugFieldName);
            if (field == null)
            {
                Console.WriteLine(
                    $"[Asher] DebugEnabler postfix reached; '{CanDebugFieldName}' field not found");
                return;
            }

            Console.WriteLine(
                $"[Asher] DebugEnabler postfix reached; {CanDebugFieldName} = {field.GetValue(null)}");
        }

        private static void PostAttachPrefix(object __instance)
        {
            if (System.Threading.Interlocked.CompareExchange(ref _postAttachPrefixLogged, 1, 0) != 0)
            {
                return;
            }

            var hasInitialized = GetInstanceFieldValue(__instance, HasInitializedFieldName);
            Console.WriteLine(
                $"[Asher] DebugEnabler post-attach reached (Game.Tick); " +
                $"Game.{HasInitializedFieldName} = {hasInitialized}");
        }

        private static void PostAttachPostfix(object __instance)
        {
            if (System.Threading.Interlocked.CompareExchange(ref _postAttachPostfixLogged, 1, 0) != 0)
            {
                return;
            }

            var field = GetStaticField(__instance?.GetType(), CanDebugFieldName);
            Console.WriteLine(
                field != null
                    ? $"[Asher] DebugEnabler real postfix executed; {CanDebugFieldName} = {field.GetValue(null)}"
                    : $"[Asher] DebugEnabler real postfix executed; '{CanDebugFieldName}' field not found");
        }

        private static FieldInfo GetStaticField(Type type, string name)
        {
            while (type != null)
            {
                var field = type.GetField(
                    name,
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.DeclaredOnly);
                if (field != null)
                {
                    return field;
                }

                type = type.BaseType;
            }

            return null;
        }

        private static object GetInstanceFieldValue(object instance, string name)
        {
            var type = instance?.GetType();
            while (type != null)
            {
                var field = type.GetField(
                    name,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.DeclaredOnly);
                if (field != null)
                {
                    return field.GetValue(instance);
                }

                type = type.BaseType;
            }

            return null;
        }

        private static string Describe(Type type)
        {
            return type != null ? type.FullName : "<unknown>";
        }
    }
}
