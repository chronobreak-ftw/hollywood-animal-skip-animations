using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace SkipAnimationsMod
{
    [BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance { get; private set; }
        public static ManualLogSource Log { get; private set; }

        private Harmony _harmony;

        private void Awake()
        {
            Instance = this;
            Log = Logger;
            SkipAnimationsPluginConfig.Init(Config);

            _harmony = new Harmony(PluginInfo.GUID);
            _harmony.PatchAll();

            Log.LogInfo($"{PluginInfo.Name} v{PluginInfo.Version} loaded.");
#if DEBUG
            Log.LogInfo(
                $"[SkipAnimations] Hotkeys: skip={SkipAnimationsPluginConfig.SkipHotkey.Value}, raidTest={SkipAnimationsPluginConfig.TriggerPoliceRaidHotkey.Value}, polluxTest={SkipAnimationsPluginConfig.TriggerPolluxNowHotkey.Value}, debug={SkipAnimationsPluginConfig.EnableDebugHotkeys.Value}"
            );
#endif
        }

        private void Update()
        {
            HotkeyController.Tick();
        }

        private void OnGUI()
        {
            HotkeyController.TickOnGui(Event.current);
        }
    }

    internal static class PluginInfo
    {
        public const string GUID = "hollywoodanimal.skipanimations";
        public const string Name = "Skip Animations";
        public const string Version = "1.0.0";
    }
}
