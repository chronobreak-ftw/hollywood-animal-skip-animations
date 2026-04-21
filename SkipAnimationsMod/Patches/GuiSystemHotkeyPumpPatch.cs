using GUISystemModule;
using HarmonyLib;

namespace SkipAnimationsMod.Patches
{
    [HarmonyPatch(typeof(GUISystem), "Update")]
    internal static class GuiSystemHotkeyPumpPatch
    {
        private static void Postfix()
        {
            HotkeyController.Tick();
        }
    }
}
