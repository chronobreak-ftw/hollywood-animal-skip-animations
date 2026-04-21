using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using Enums;
using GUISystemModule;
using UI.Views;
using UnityEngine;
#if DEBUG
using Data.GameObject;
using GameEvents;
using Managers;
using Model;
using Zenject;
using SkipAnimationsMod.IntegrationTests;
#endif

namespace SkipAnimationsMod
{
    internal static class HotkeyController
    {
        private const int UpdateLoopConfirmTicks = 180;

        private static int _updateTicks;
        private static bool _updateLoopConfirmed;
        private static int _lastTickFrame = -1;

        public static void Tick()
        {
            int frame = Time.frameCount;
            if (frame == _lastTickFrame)
            {
                return;
            }
            _lastTickFrame = frame;

            _updateTicks++;
            if (
                !_updateLoopConfirmed
                && SkipAnimationsPluginConfig.EnableInputDiagnostics.Value
                && _updateTicks > UpdateLoopConfirmTicks
            )
            {
                _updateLoopConfirmed = true;
                Plugin.Log?.LogInfo("[SkipAnimations] Update loop is active.");
            }

            if (
                SkipAnimationsPluginConfig.EnableEscSkip.Value
                && IsShortcutDown(SkipAnimationsPluginConfig.SkipHotkey.Value)
            )
            {
                TrySkipCurrentScene();
            }

#if DEBUG
            if (!SkipAnimationsPluginConfig.EnableDebugHotkeys.Value)
            {
                return;
            }

            if (
                SkipAnimationsPluginConfig.EnableIntegrationTests.Value
                && IsShortcutDown(SkipAnimationsPluginConfig.RunIntegrationTestsHotkey.Value)
            )
            {
                Plugin.Log?.LogInfo(
                    "[SkipAnimations] Integration tests hotkey detected via Update."
                );
                IntegrationTestRunner.RunSmokeTests();
            }

            if (IsShortcutDown(SkipAnimationsPluginConfig.TriggerPoliceRaidHotkey.Value))
            {
                Plugin.Log?.LogInfo("[SkipAnimations] Police raid hotkey detected via Update.");
                TryTriggerPoliceRaid();
            }

            if (IsShortcutDown(SkipAnimationsPluginConfig.TriggerPolluxNowHotkey.Value))
            {
                Plugin.Log?.LogInfo("[SkipAnimations] Pollux hotkey detected via Update.");
                TryTriggerPolluxNow();
            }
#endif
        }

        public static void TickOnGui(Event evt)
        {
            if (evt == null || evt.type != EventType.KeyDown)
            {
                return;
            }

            if (SkipAnimationsPluginConfig.EnableInputDiagnostics.Value)
            {
                Plugin.Log?.LogInfo(
                    $"[SkipAnimations] OnGUI keydown: key={evt.keyCode}, ctrl={evt.control}, shift={evt.shift}, alt={evt.alt}, cmd={evt.command}"
                );
            }

            if (
                SkipAnimationsPluginConfig.EnableEscSkip.Value
                && IsShortcutPressedByEvent(SkipAnimationsPluginConfig.SkipHotkey.Value, evt)
            )
            {
                Plugin.Log?.LogInfo("[SkipAnimations] Skip hotkey detected via OnGUI.");
                TrySkipCurrentScene();
            }

#if DEBUG
            if (!SkipAnimationsPluginConfig.EnableDebugHotkeys.Value)
            {
                return;
            }

            if (
                SkipAnimationsPluginConfig.EnableIntegrationTests.Value
                && IsShortcutPressedByEvent(
                    SkipAnimationsPluginConfig.RunIntegrationTestsHotkey.Value,
                    evt
                )
            )
            {
                Plugin.Log?.LogInfo(
                    "[SkipAnimations] Integration tests hotkey detected via OnGUI."
                );
                IntegrationTestRunner.RunSmokeTests();
            }

            if (
                IsShortcutPressedByEvent(
                    SkipAnimationsPluginConfig.TriggerPoliceRaidHotkey.Value,
                    evt
                )
            )
            {
                Plugin.Log?.LogInfo("[SkipAnimations] Police raid hotkey detected via OnGUI.");
                TryTriggerPoliceRaid();
            }

            if (
                IsShortcutPressedByEvent(
                    SkipAnimationsPluginConfig.TriggerPolluxNowHotkey.Value,
                    evt
                )
            )
            {
                Plugin.Log?.LogInfo("[SkipAnimations] Pollux hotkey detected via OnGUI.");
                TryTriggerPolluxNow();
            }
#endif
        }

        private static void TrySkipCurrentScene()
        {
            GUISystem gui = TryGetGuiSystem();
            if (gui == null)
            {
                return;
            }

            PoliceRaidAnimation activeRaidAnimation = Resources
                .FindObjectsOfTypeAll<PoliceRaidAnimation>()
                .FirstOrDefault(x => x != null && x.gameObject.activeInHierarchy);

            if (activeRaidAnimation != null)
            {
                Action startCallback = RaidSkipState.PendingRaidStartCallback;
                if (startCallback != null)
                {
                    RaidSkipState.PendingRaidStartCallback = null;
                    CancelRaidAnimationDisposables(activeRaidAnimation);
                    ReflectionHelper.InvokeHidden(activeRaidAnimation, "ResetLabel");
                    ReflectionHelper.InvokeHidden(activeRaidAnimation, "StopSideFlashers");
                    try
                    {
                        startCallback.Invoke();
                    }
                    catch (Exception ex)
                    {
                        Plugin.Log?.LogError(
                            $"[SkipAnimations] Failed invoking raid start callback: {ex}"
                        );
                    }
                    Plugin.Log?.LogInfo(
                        "[SkipAnimations] Skipped raid start animation -> jumped to results."
                    );
                }
                else
                {
                    CancelRaidAnimationDisposables(activeRaidAnimation);
                    ReflectionHelper.InvokeHidden(
                        activeRaidAnimation,
                        "<PlayEnd>g__OnFinished|41_2"
                    );
                    Plugin.Log?.LogInfo(
                        "[SkipAnimations] Skipped raid end animation -> raid fully finished."
                    );
                }
                return;
            }

            if (gui.IsViewShown(ViewKeys.PolluxCeremonyView))
            {
                gui.GetView(ViewKeys.PolluxCeremonyView)?.CloseAnimated(null);
                Plugin.Log?.LogInfo("[SkipAnimations] Skipped Pollux ceremony.");
                return;
            }

            if (gui.IsViewShown(ViewKeys.PoliceRaidResultsView))
            {
                object view = gui.GetView(ViewKeys.PoliceRaidResultsView);
                ReflectionHelper.InvokeHidden(view, "OnRaidFinished");
                Plugin.Log?.LogInfo("[SkipAnimations] Skipped police raid results animation.");
                return;
            }

            if (gui.IsViewShown(ViewKeys.PoliceRaidEventView))
            {
                object view = gui.GetView(ViewKeys.PoliceRaidEventView);
                Action callback = ReflectionHelper.GetHiddenField<Action>(view, "callback");
                callback?.Invoke();
                ReflectionHelper.InvokeHidden(view, "CloseAnimated", (object)null);
                Plugin.Log?.LogInfo("[SkipAnimations] Skipped police raid event panel.");
            }
        }

        private static void CancelRaidAnimationDisposables(PoliceRaidAnimation anim)
        {
            var animDisp = ReflectionHelper.GetHiddenField<List<IDisposable>>(
                anim,
                "animationDisposables"
            );
            if (animDisp != null)
            {
                foreach (var d in animDisp)
                    d?.Dispose();
                animDisp.Clear();
            }

            var startDisp = ReflectionHelper.GetHiddenField<List<IDisposable>>(
                anim,
                "startAnimationDisposables"
            );
            if (startDisp != null)
            {
                foreach (var d in startDisp)
                    d?.Dispose();
                startDisp.Clear();
            }
        }

        private static GUISystem TryGetGuiSystem()
        {
            return Resources.FindObjectsOfTypeAll<GUISystem>().FirstOrDefault();
        }

        private static bool IsShortcutDown(KeyboardShortcut shortcut)
        {
            return shortcut.IsDown();
        }

        private static bool IsShortcutPressedByEvent(KeyboardShortcut shortcut, Event evt)
        {
            if (shortcut.MainKey == KeyCode.None || evt.keyCode != shortcut.MainKey)
            {
                return false;
            }

            KeyCode[] modifiers =
                shortcut.Modifiers != null ? shortcut.Modifiers.ToArray() : Array.Empty<KeyCode>();

            if (modifiers.Length == 0)
            {
                return !evt.alt && !evt.control && !evt.shift && !evt.command;
            }

            bool needCtrl = modifiers.Any(IsControlModifier);
            bool needShift =
                modifiers.Contains(KeyCode.LeftShift) || modifiers.Contains(KeyCode.RightShift);
            bool needAlt =
                modifiers.Contains(KeyCode.LeftAlt) || modifiers.Contains(KeyCode.RightAlt);
            bool needCmd =
                modifiers.Contains(KeyCode.LeftCommand) || modifiers.Contains(KeyCode.RightCommand);

            return evt.control == needCtrl
                && evt.shift == needShift
                && evt.alt == needAlt
                && evt.command == needCmd;
        }

        private static bool IsControlModifier(KeyCode code)
        {
            return code == KeyCode.LeftControl || code == KeyCode.RightControl;
        }

#if DEBUG
        internal static void EnsureRuntimeManagersForTest() => EnsureRuntimeManagers();

        private static void TryTriggerPoliceRaid()
        {
            EnsureRuntimeManagers();

            if (RuntimeState.PoliceRaidManager == null)
            {
                Plugin.Log?.LogWarning("[SkipAnimations] PoliceRaidManager unavailable yet.");
                return;
            }

            RuntimeState.PoliceRaidManager.DebugStartRaid();
            Plugin.Log?.LogInfo("[SkipAnimations] Triggered police raid (debug hotkey).");
        }

        private static void TryTriggerPolluxNow()
        {
            EnsureRuntimeManagers();

            if (
                RuntimeState.AssociationManager == null
                || RuntimeState.GameEventManager == null
                || RuntimeState.GameStateManager == null
                || RuntimeState.MoviesManager == null
                || RuntimeState.TimeManager == null
            )
            {
                Plugin.Log?.LogWarning(
                    "[SkipAnimations] Required managers unavailable yet (AssociationManager / GameEventManager / GameStateManager / MoviesManager / TimeManager)."
                );
                return;
            }

            if (!RuntimeState.GameStateManager.State.associationIsFounded)
            {
                Plugin.Log?.LogWarning(
                    "[SkipAnimations] Association is not founded yet. Pollux debug trigger is disabled to avoid mutating campaign state."
                );
                return;
            }

            if (!EnsurePolluxHasTestData())
            {
                return;
            }

            GameEventProcessor ceremonyProcessor = FindAnyPolluxCeremonyEvent();
            if (ceremonyProcessor == null)
            {
                Plugin.Log?.LogWarning(
                    "[SkipAnimations] No POLLUX_<year> event found in loaded event data. Cannot trigger ceremony."
                );
                return;
            }

            GUISystem gui = TryGetGuiSystem();
            if (gui == null)
            {
                Plugin.Log?.LogWarning("[SkipAnimations] GUISystem not found.");
                return;
            }

            GUIParams param = new GUIParams
            {
                { GUIParamTypes.Event, ceremonyProcessor },
                { GUIParamTypes.WithAnimation, null },
                { GUIParamTypes.PauseTime, null },
            };
            gui.ShowView(ViewKeys.PolluxCeremonyView, param);

            gui.AddToQueue(
                UIQueuePriority.Instant,
                ViewKeys.PolluxResultsView,
                new GUIParams
                {
                    { GUIParamTypes.WithAnimation, null },
                    { GUIParamTypes.PauseTime, null },
                }
            );

            Plugin.Log?.LogInfo(
                "[SkipAnimations] Triggered Pollux ceremony (debug hotkey). Press skip hotkey to close it."
            );
        }

        private static GameEventProcessor FindAnyPolluxCeremonyEvent()
        {
            int currentYear = RuntimeState.TimeManager.CurrentTime.Year;
            GameEventProcessor currentYearProcessor =
                RuntimeState.GameEventManager.GetPolluxCeremonyEventProcessorForYear(currentYear);
            if (currentYearProcessor != null)
            {
                return currentYearProcessor;
            }

            var allHeaders = ReflectionHelper.GetHiddenField<System.Collections.Generic.Dictionary<
                EventContextType,
                System.Collections.Generic.Dictionary<string, GameEventHeader[]>
            >>(RuntimeState.GameEventManager, "allEventsHeaders");

            if (allHeaders == null)
            {
                Plugin.Log?.LogWarning("[SkipAnimations] allEventsHeaders field not found.");
                return null;
            }

            System.Collections.Generic.Dictionary<string, GameEventHeader[]> polluxEvents;
            if (!allHeaders.TryGetValue(EventContextType.PolluxCeremony, out polluxEvents))
            {
                Plugin.Log?.LogWarning(
                    "[SkipAnimations] No PolluxCeremony context in event headers."
                );
                return null;
            }

            foreach (string key in polluxEvents.Keys)
            {
                if (!key.StartsWith("POLLUX_"))
                    continue;

                Plugin.Log?.LogInfo(
                    $"[SkipAnimations] Using Pollux event '{key}' for ceremony test."
                );

                if (
                    int.TryParse(key.Substring("POLLUX_".Length), out int year)
                    && RuntimeState.GameEventManager.GetPolluxCeremonyEventProcessorForYear(year)
                        is GameEventProcessor proc
                    && proc != null
                )
                {
                    return proc;
                }
            }

            return null;
        }

        private static void EnsureRuntimeManagers()
        {
            if (
                RuntimeState.PoliceRaidManager != null
                && RuntimeState.AssociationManager != null
                && RuntimeState.MoviesManager != null
                && RuntimeState.ViewController != null
                && RuntimeState.GameEventManager != null
                && RuntimeState.GameStateManager != null
                && RuntimeState.TimeManager != null
            )
            {
                return;
            }

            ProjectContext projectContext = ProjectContext.Instance;
            DiContainer container = projectContext != null ? projectContext.Container : null;
            if (container == null)
            {
                return;
            }

            try
            {
                RuntimeState.PoliceRaidManager =
                    RuntimeState.PoliceRaidManager ?? container.Resolve<PoliceRaidManager>();
                RuntimeState.AssociationManager =
                    RuntimeState.AssociationManager ?? container.Resolve<AssociationManager>();
                RuntimeState.MoviesManager =
                    RuntimeState.MoviesManager ?? container.Resolve<MoviesManager>();
                RuntimeState.ViewController =
                    RuntimeState.ViewController ?? container.Resolve<ViewController>();
                RuntimeState.GameEventManager =
                    RuntimeState.GameEventManager ?? container.Resolve<GameEventManager>();
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"[SkipAnimations] Resolve managers failed: {ex.Message}");
            }

            if (RuntimeState.AssociationManager != null)
            {
                RuntimeState.GameStateManager =
                    RuntimeState.GameStateManager
                    ?? ReflectionHelper.GetHiddenField<GameStateManager>(
                        RuntimeState.AssociationManager,
                        "gameStateManager"
                    );
                RuntimeState.TimeManager =
                    RuntimeState.TimeManager
                    ?? ReflectionHelper.GetHiddenField<TimeManager>(
                        RuntimeState.AssociationManager,
                        "timeManager"
                    );
            }
        }

        private static bool EnsurePolluxHasTestData()
        {
            int currentYear = RuntimeState.TimeManager.CurrentTime.Year;

            if (!RuntimeState.AssociationManager.PolluxHistory.ContainsKey(currentYear))
            {
                RuntimeState.AssociationManager.PolluxHistory[currentYear] =
                    new AssociationManager.PolluxResults();
                Plugin.Log?.LogInfo(
                    $"[SkipAnimations] Created PolluxHistory entry for year {currentYear}."
                );
            }

            AssociationManager.PolluxResults pollux = RuntimeState
                .AssociationManager
                .LastPolluxResults;
            if (pollux == null)
            {
                Plugin.Log?.LogWarning(
                    "[SkipAnimations] LastPolluxResults still null after creating history entry - skipping data seed."
                );
                return false;
            }

            EnsurePolluxDictionaries(pollux);

            bool hasAnyNominees = pollux.nominees.Any(kv => kv.Value != null && kv.Value.Count > 0);
            if (hasAnyNominees)
            {
                return true;
            }

            ReflectionHelper.InvokeHidden(
                RuntimeState.AssociationManager,
                "OnPolluxNominationDay",
                true
            );
            pollux = RuntimeState.AssociationManager.LastPolluxResults;
            if (pollux != null)
            {
                EnsurePolluxDictionaries(pollux);

                hasAnyNominees = pollux.nominees.Any(kv => kv.Value != null && kv.Value.Count > 0);
                if (hasAnyNominees)
                {
                    Plugin.Log?.LogInfo(
                        "[SkipAnimations] Generated Pollux data through AssociationManager.OnPolluxNominationDay."
                    );
                    return true;
                }
            }

            int movieId = RuntimeState
                .MoviesManager.AllMovies.Where(m => m.Id > 0)
                .Select(m => m.Id)
                .FirstOrDefault();
            if (movieId <= 0)
            {
                Plugin.Log?.LogWarning(
                    "[SkipAnimations] No movies available to seed Pollux debug data. Continuing with empty Pollux results."
                );
                pollux = RuntimeState.AssociationManager.LastPolluxResults;
                if (pollux == null)
                {
                    pollux = new AssociationManager.PolluxResults();
                    RuntimeState.AssociationManager.PolluxHistory[currentYear] = pollux;
                }

                EnsurePolluxDictionaries(pollux);
                return true;
            }

            PolluxPretender fakeMoviePretender = new PolluxPretender
            {
                category = PolluxNominationCategories.BEST_MOVIE,
                movieId = movieId,
                talentIds = new List<int>(),
            };

            pollux.nominees[PolluxNominationCategories.BEST_MOVIE] = new List<
                KeyValuePair<float, PolluxPretender>
            >
            {
                new KeyValuePair<float, PolluxPretender>(5f, fakeMoviePretender),
            };
            pollux.winners[PolluxNominationCategories.BEST_MOVIE] = fakeMoviePretender;
            Plugin.Log?.LogInfo(
                "[SkipAnimations] Seeded Pollux test data (BEST_MOVIE nominee + winner)."
            );
            return true;
        }

        private static void EnsurePolluxDictionaries(AssociationManager.PolluxResults pollux)
        {
            pollux.nominees =
                pollux.nominees
                ?? new Dictionary<
                    PolluxNominationCategories,
                    List<KeyValuePair<float, PolluxPretender>>
                >();
            pollux.winners =
                pollux.winners ?? new Dictionary<PolluxNominationCategories, PolluxPretender>();
            pollux.moodShifts =
                pollux.moodShifts
                ?? new Dictionary<PolluxNominationCategories, Dictionary<int, Vector2>>();
        }
#endif
    }
}
