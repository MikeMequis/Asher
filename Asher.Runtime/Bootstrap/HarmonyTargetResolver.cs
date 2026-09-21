using System;
using System.Reflection;

namespace Asher.Runtime.Bootstrap
{
    /// <summary>
    /// Generic Harmony target resolution for the runtime's orchestration.
    ///
    /// Reflection can return an inherited method (ReflectedType differs from DeclaringType) and
    /// Harmony only patches declared members, so the method is re-resolved from its declaring
    /// type (e.g. Dust.Game1.Initialize -> Microsoft.Xna.Framework.Game.Initialize). When the
    /// type declares its own method the result is unchanged. Returns null when not found.
    /// </summary>
    internal static class HarmonyTargetResolver
    {
        private const BindingFlags Flags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public static MethodInfo ResolveDeclared(Type type, string methodName)
        {
            if (type == null || string.IsNullOrEmpty(methodName))
                return null;

            var candidate = type.GetMethod(methodName, Flags);
            if (candidate == null)
                return null;

            var declaringType = candidate.DeclaringType;
            if (declaringType == null || candidate.ReflectedType == declaringType)
                return candidate;

            var declared = declaringType.GetMethod(
                candidate.Name,
                Flags | BindingFlags.DeclaredOnly);
            if (declared != null)
                return declared;

            var baseDefinition = candidate.GetBaseDefinition();
            if (baseDefinition != null && baseDefinition.DeclaringType != null)
            {
                declared = baseDefinition.DeclaringType.GetMethod(
                    baseDefinition.Name,
                    Flags | BindingFlags.DeclaredOnly);
                if (declared != null)
                    return declared;
            }

            return candidate;
        }
    }
}
