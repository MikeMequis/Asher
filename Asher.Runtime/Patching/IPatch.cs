using HarmonyLib;

namespace Asher.Runtime.Patching
{
    internal interface IPatch
    {
        void Apply(Harmony harmony);
    }
}
