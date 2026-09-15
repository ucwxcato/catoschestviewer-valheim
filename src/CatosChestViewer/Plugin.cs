using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace CatosChestViewer
{
    [BepInPlugin(Guid, Name, Version)]
    [BepInProcess("valheim.exe")]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string Guid = "com.catosaur.catoschestviewer";
        public const string Name = "Catos Chest Viewer";
        public const string Version = "0.1.1";

        internal static ManualLogSource Log { get; private set; }

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;
            ModConfig.Bind(Config);

            _harmony = new Harmony(Guid);
            _harmony.PatchAll(typeof(ChestViewerPatches));

            Logger.LogInfo($"{Name} {Version} client-only loaded.");
        }

        // Run after Valheim's normal UpdateCrosshair/UI work. This keeps the
        // display alive even when another HUD postfix runs after our Harmony
        // patch and replaces the native hover text.
        private void LateUpdate()
        {
            ChestOverlay.Apply(Hud.instance, Player.m_localPlayer);
        }

        private void OnDestroy()
        {
            ChestOverlay.Clear();
            _harmony?.UnpatchAll(Guid);
        }
    }
}
