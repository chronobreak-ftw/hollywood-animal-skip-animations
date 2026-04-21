using System;
using HarmonyLib;
using UI.Views;

namespace SkipAnimationsMod.Patches
{
    [HarmonyPatch(typeof(PoliceRaidAnimation), "PlayStart")]
    internal static class PoliceRaidAnimationSkipPatch
    {
        private static void Prefix(Action onFinished)
        {
            RaidSkipState.PendingRaidStartCallback = onFinished;
        }
    }
}
