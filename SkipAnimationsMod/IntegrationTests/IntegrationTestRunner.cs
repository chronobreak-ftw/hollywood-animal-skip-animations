using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using GUISystemModule;
using Managers;
using Model;
using UnityEngine;
using Zenject;

namespace SkipAnimationsMod.IntegrationTests
{
    internal static class IntegrationTestRunner
    {
        public static void RunSmokeTests()
        {
            var checks = new List<(string Name, Func<bool> Test)>
            {
                ("Config initialized", IsConfigInitialized),
                ("Release-safe debug defaults", HasReleaseSafeDefaults),
                ("ProjectContext/DI container available", HasProjectContextContainer),
                ("GUISystem discovered", CanFindGuiSystem),
                // All game managers live in scene containers, not ProjectContext — verify via patch capture + reflection.
                ("Core managers captured via patch/reflection", HaveAllRuntimeManagers),
            };

            int passed = 0;
            int failed = 0;

            foreach (var check in checks)
            {
                bool ok = false;
                string details = string.Empty;

                try
                {
                    ok = check.Test();
                }
                catch (Exception ex)
                {
                    details = $" Exception: {ex.GetType().Name} - {ex.Message}";
                }

                if (ok)
                {
                    passed++;
                    Plugin.Log?.LogInfo($"[SkipAnimations][IT] PASS: {check.Name}");
                }
                else
                {
                    failed++;
                    Plugin.Log?.LogWarning($"[SkipAnimations][IT] FAIL: {check.Name}.{details}");
                }
            }

            Plugin.Log?.LogInfo(
                $"[SkipAnimations][IT] Completed. Passed={passed}, Failed={failed}, Total={checks.Count}."
            );
        }

        private static bool IsConfigInitialized()
        {
            return SkipAnimationsPluginConfig.SkipHotkey != null
                && SkipAnimationsPluginConfig.TriggerPoliceRaidHotkey != null
                && SkipAnimationsPluginConfig.TriggerPolluxNowHotkey != null
                && SkipAnimationsPluginConfig.EnableIntegrationTests != null;
        }

        private static bool HasReleaseSafeDefaults()
        {
            return !GetDefaultValue(SkipAnimationsPluginConfig.EnableDebugHotkeys)
                && !GetDefaultValue(SkipAnimationsPluginConfig.EnableInputDiagnostics)
                && !GetDefaultValue(SkipAnimationsPluginConfig.EnableIntegrationTests);
        }

        private static bool HasProjectContextContainer()
        {
            return ProjectContext.Instance != null && ProjectContext.Instance.Container != null;
        }

        private static bool CanFindGuiSystem()
        {
            return Resources.FindObjectsOfTypeAll<GUISystem>().FirstOrDefault() != null;
        }

        // All game managers live in scene containers, not ProjectContext.
        // The mod captures them via Harmony Initialize patches; this verifies those captures fired.
        private static bool HaveAllRuntimeManagers()
        {
            HotkeyController.EnsureRuntimeManagersForTest();
            bool ok =
                RuntimeState.PoliceRaidManager != null
                && RuntimeState.AssociationManager != null
                && RuntimeState.MoviesManager != null
                && RuntimeState.GameEventManager != null
                && RuntimeState.GameStateManager != null
                && RuntimeState.TimeManager != null;

            if (!ok)
            {
                Plugin.Log?.LogWarning(
                    $"[SkipAnimations][IT] Missing managers: "
                        + $"Raid={RuntimeState.PoliceRaidManager != null}, "
                        + $"Assoc={RuntimeState.AssociationManager != null}, "
                        + $"Movies={RuntimeState.MoviesManager != null}, "
                        + $"Events={RuntimeState.GameEventManager != null}, "
                        + $"State={RuntimeState.GameStateManager != null}, "
                        + $"Time={RuntimeState.TimeManager != null}"
                );
            }
            return ok;
        }

        private static bool GetDefaultValue(ConfigEntry<bool> entry)
        {
            if (entry == null)
            {
                return false;
            }

            return Convert.ToBoolean(entry.DefaultValue);
        }
    }
}
