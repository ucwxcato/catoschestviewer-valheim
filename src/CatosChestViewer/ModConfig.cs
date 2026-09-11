using BepInEx.Configuration;

namespace CatosChestViewer
{
    internal static class ModConfig
    {
        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<int> UpdateIntervalMs;
        internal static ConfigEntry<int> MaxLines;
        internal static ConfigEntry<int> MaxTextCharacters;
        internal static ConfigEntry<bool> ShowHeader;
        internal static ConfigEntry<bool> ShowEmptyMessage;
        internal static ConfigEntry<string> EmptyMessage;

        internal static void Bind(ConfigFile config)
        {
            Enabled = config.Bind("General", "Enabled", true,
                "Enable the client-only chest contents display.");
            UpdateIntervalMs = config.Bind("General", "UpdateIntervalMs", 100,
                new ConfigDescription("Minimum milliseconds between content refresh checks.",
                    new AcceptableValueRange<int>(25, 500)));
            MaxLines = config.Bind("Display", "MaxLines", 30,
                new ConfigDescription("Maximum number of item lines to display.",
                    new AcceptableValueRange<int>(1, 100)));
            MaxTextCharacters = config.Bind("Display", "MaxTextCharacters", 1200,
                new ConfigDescription("Maximum formatted hover-text length.",
                    new AcceptableValueRange<int>(100, 4096)));
            ShowHeader = config.Bind("Display", "ShowHeader", true,
                "Show the chest name above the contents.");
            ShowEmptyMessage = config.Bind("Display", "ShowEmptyMessage", true,
                "Show Empty when the targeted chest has no items.");
            EmptyMessage = config.Bind("Display", "EmptyMessage", "Empty",
                "Text shown for an empty chest when ShowEmptyMessage is enabled.");
        }
    }
}
