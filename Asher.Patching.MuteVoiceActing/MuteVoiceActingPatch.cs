using Asher.SDK.Logging;
using Asher.SDK.Patching;
using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;

namespace Asher.Patching.MuteVoiceActing
{
    /// <summary>
    /// Mutes voice acting by forcing the XACT Voice category volume to zero
    /// whenever SFX volume is updated.
    /// </summary>
    public sealed class MuteVoiceActingPatch : IAsherPatchModule
    {
        private static object? _audioEngine;

        public static bool Enabled { get; set; }

        public string Name => "Voice Acting Muter";

        public void Apply(Harmony harmony)
        {
            if (!Enabled)
                return;

            try
            {
                var soundType = GetSoundType();
                var setSfxVolumeMethod = soundType?.GetMethod(
                    "SetSFXVolume",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                if (setSfxVolumeMethod == null)
                {
                    AsherLog.Warning("[MuteVoiceActing] Não foi possível encontrar Sound.SetSFXVolume");
                    return;
                }

                harmony.Patch(
                    setSfxVolumeMethod,
                    postfix: new HarmonyMethod(typeof(MuteVoiceActingPatch), nameof(SetSfxVolumePostfix)));
            }
            catch (Exception ex)
            {
                AsherLog.Error($"[MuteVoiceActing] Erro ao aplicar patch: {ex.Message}");
            }
        }

        private static void SetSfxVolumePostfix()
        {
            try
            {
                if (_audioEngine == null)
                {
                    var soundType = GetSoundType();
                    var engineField = soundType?.GetField(
                        "engine",
                        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                    _audioEngine = engineField?.GetValue(null);
                }

                if (_audioEngine == null)
                    return;

                var getCategoryMethod = _audioEngine.GetType().GetMethod("GetCategory");
                var voiceCategory = getCategoryMethod?.Invoke(_audioEngine, new object[] { "Voice" });
                voiceCategory?.GetType()
                    .GetMethod("SetVolume")
                    ?.Invoke(voiceCategory, new object[] { 0f });
            }
            catch (Exception ex)
            {
                AsherLog.Error($"[MuteVoiceActing] Erro durante execução: {ex.Message}");
            }
        }

        private static Type? GetSoundType()
        {
            return AppDomain.CurrentDomain
                .GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "DustAET")
                ?.GetType("Dust.Audio.Sound");
        }
    }
}
