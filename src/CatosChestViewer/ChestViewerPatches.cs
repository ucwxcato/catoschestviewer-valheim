using HarmonyLib;
using System;

namespace CatosChestViewer
{
    [HarmonyPatch(typeof(Hud), "UpdateCrosshair")]
    internal static class ChestViewerPatches
    {
        private static void Postfix(Hud __instance, Player player, float bowDrawPercentage)
        {
            try
            {
                ChestOverlay.Apply(__instance, player);
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Chest hover update failed: {ex}");
                ChestOverlay.Clear();
            }
        }
    }
}
