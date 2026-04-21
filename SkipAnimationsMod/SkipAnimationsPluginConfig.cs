using BepInEx.Configuration;
using UnityEngine;

namespace SkipAnimationsMod
{
    internal static class SkipAnimationsPluginConfig
    {
        public static ConfigEntry<bool> EnableEscSkip { get; private set; }
        public static ConfigEntry<KeyboardShortcut> SkipHotkey { get; private set; }
        public static ConfigEntry<bool> EnableInputDiagnostics { get; private set; }

#if DEBUG
        public static ConfigEntry<bool> EnableDebugHotkeys { get; private set; }
        public static ConfigEntry<KeyboardShortcut> TriggerPoliceRaidHotkey { get; private set; }
        public static ConfigEntry<KeyboardShortcut> TriggerPolluxNowHotkey { get; private set; }
        public static ConfigEntry<bool> EnableIntegrationTests { get; private set; }
        public static ConfigEntry<KeyboardShortcut> RunIntegrationTestsHotkey { get; private set; }
#endif

        public static void Init(ConfigFile config)
        {
            EnableEscSkip = config.Bind(
                "Skip",
                "EnableEscSkip",
                true,
                "If true, pressing SkipHotkey attempts to skip currently shown raid/pollux scenes."
            );

            SkipHotkey = config.Bind(
                "Skip",
                "SkipHotkey",
                new KeyboardShortcut(KeyCode.Escape),
                "Hotkey used to skip current skippable scene."
            );

            EnableInputDiagnostics = config.Bind(
                "Diagnostics",
                "EnableInputDiagnostics",
                false,
                "If true, logs every keydown event and hotkey detection. Only needed when troubleshooting input issues."
            );

#if DEBUG
            EnableDebugHotkeys = config.Bind(
                "Debug",
                "EnableDebugHotkeys",
                false,
                "If true, debug hotkeys can trigger raid/pollux scenes for testing. Keep disabled for normal gameplay."
            );

            TriggerPoliceRaidHotkey = config.Bind(
                "Debug",
                "TriggerPoliceRaidHotkey",
                new KeyboardShortcut(KeyCode.F9),
                "Force-start police raid for testing."
            );

            TriggerPolluxNowHotkey = config.Bind(
                "Debug",
                "TriggerPolluxNowHotkey",
                new KeyboardShortcut(KeyCode.F10),
                "Generate Pollux nominees/winners and immediately start ceremony/results flow."
            );

            EnableIntegrationTests = config.Bind(
                "Debug",
                "EnableIntegrationTests",
                false,
                "If true, enables manual integration smoke tests in Debug builds."
            );

            RunIntegrationTestsHotkey = config.Bind(
                "Debug",
                "RunIntegrationTestsHotkey",
                new KeyboardShortcut(KeyCode.F8),
                "Runs integration smoke tests and logs pass/fail output."
            );
#endif
        }
    }
}
