using HarmonyLib;

namespace CatosChestViewer
{
    [HarmonyPatch(typeof(Hud), "UpdateCrosshair")]
    internal static class ChestViewerPatches
    {
        private static void Postfix(Hud __instance, Player player)
        {
            ChestOverlay.Apply(__instance, player);
        }
    }
}
