#if DEBUG
using GameEvents;
using HarmonyLib;
using Managers;
using Model;

namespace SkipAnimationsMod.Patches
{
    [HarmonyPatch(typeof(PoliceRaidManager), nameof(PoliceRaidManager.Initialize))]
    internal static class PoliceRaidManagerCapturePatch
    {
        private static void Postfix(PoliceRaidManager __instance)
        {
            RuntimeState.PoliceRaidManager = __instance;
        }
    }

    [HarmonyPatch(typeof(AssociationManager), nameof(AssociationManager.Initialize))]
    internal static class AssociationManagerCapturePatch
    {
        private static void Postfix(AssociationManager __instance)
        {
            RuntimeState.AssociationManager = __instance;
        }
    }

    [HarmonyPatch(typeof(ViewController), nameof(ViewController.Initialize))]
    internal static class ViewControllerCapturePatch
    {
        private static void Postfix(ViewController __instance)
        {
            RuntimeState.ViewController = __instance;
        }
    }

    [HarmonyPatch(typeof(MoviesManager), nameof(MoviesManager.Initialize))]
    internal static class MoviesManagerCapturePatch
    {
        private static void Postfix(MoviesManager __instance)
        {
            RuntimeState.MoviesManager = __instance;
        }
    }

    [HarmonyPatch(typeof(GameEventManager), nameof(GameEventManager.Initialize))]
    internal static class GameEventManagerCapturePatch
    {
        private static void Postfix(GameEventManager __instance)
        {
            RuntimeState.GameEventManager = __instance;
        }
    }

    [HarmonyPatch(typeof(TimeManager), nameof(TimeManager.Initialize))]
    internal static class TimeManagerCapturePatch
    {
        private static void Postfix(TimeManager __instance)
        {
            RuntimeState.TimeManager = __instance;
        }
    }
}
#endif
