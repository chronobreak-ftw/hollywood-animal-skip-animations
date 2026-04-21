using System;

namespace SkipAnimationsMod
{
    // Holds transient state for the production police-raid skip path.
    // PoliceRaidAnimationSkipPatch captures the onFinished callback here when PlayStart fires;
    // TrySkipCurrentScene reads and clears it when the player presses the skip hotkey.
    internal static class RaidSkipState
    {
        public static Action PendingRaidStartCallback;
    }
}
