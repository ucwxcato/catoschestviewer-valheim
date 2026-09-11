using BepInEx;

namespace CatosChestViewer
{
    [BepInPlugin(Guid, Name, Version)]
    [BepInProcess("valheim.exe")]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string Guid = "com.catosaur.catoschestviewer";
        public const string Name = "Catos Chest Viewer";
        public const string Version = "0.1.0";

        private void Awake()
        {
            Logger.LogInfo($"{Name} {Version} bootstrap loaded. Chest display is not implemented yet.");
        }
    }
}
