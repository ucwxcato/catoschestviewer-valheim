using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace CatosChestViewer
{
    internal sealed class ChestContentsSnapshot
    {
        internal ChestContentsSnapshot(List<ChestItemEntry> items, string fingerprint)
        {
            Items = items.AsReadOnly();
            Fingerprint = fingerprint;
        }

        internal IReadOnlyList<ChestItemEntry> Items { get; }
        internal string Fingerprint { get; }
    }

    internal sealed class ChestItemEntry
    {
        internal ChestItemEntry(string name, int stack)
        {
            Name = name;
            Stack = stack;
        }

        internal string Name { get; }
        internal int Stack { get; }
    }

    internal static class ChestInventoryReader
    {
        private static readonly Type LocalizationType = typeof(Container).Assembly.GetType("Localization");
        private static readonly PropertyInfo LocalizationInstance = LocalizationType?.GetProperty(
            "instance", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly MethodInfo LocalizeMethod = LocalizationType?.GetMethod(
            "Localize", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            null, new[] { typeof(string) }, null);
        private static float _nextReadWarningTime;

        internal static bool TryRead(Container container, out ChestContentsSnapshot snapshot)
        {
            snapshot = null;
            if (!container) return false;

            try
            {
                Inventory inventory = container.GetInventory();
                if (inventory == null) return false;

                List<ItemDrop.ItemData> items = inventory.GetAllItemsInGridOrder();
                var entries = new List<ChestItemEntry>();
                string fingerprint = BuildFingerprint(items, entries);
                snapshot = new ChestContentsSnapshot(entries, fingerprint);
                return true;
            }
            catch (Exception ex)
            {
                if (Time.unscaledTime >= _nextReadWarningTime)
                {
                    _nextReadWarningTime = Time.unscaledTime + 2f;
                    Plugin.Log?.LogWarning($"Chest inventory read failed: {ex.Message}");
                }
                return false;
            }
        }

        private static string BuildFingerprint(List<ItemDrop.ItemData> items, List<ChestItemEntry> entries)
        {
            if (items == null || items.Count == 0) return "empty";

            var fingerprint = new StringBuilder();
            foreach (ItemDrop.ItemData item in items)
            {
                if (item == null || item.m_shared == null) continue;

                string name = item.m_shared.m_name ?? string.Empty;
                int stack = item.m_stack;
                entries.Add(new ChestItemEntry(Localize(name), stack));
                fingerprint.Append(name).Append('\u001f').Append(stack).Append('\u001e');
            }

            return fingerprint.ToString();
        }

        internal static string Localize(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "Unknown item";

            try
            {
                object localization = LocalizationInstance?.GetValue(null, null);
                if (localization != null && LocalizeMethod != null)
                    return LocalizeMethod.Invoke(localization, new object[] { value }) as string ?? value;
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"Item localization failed: {ex.Message}");
            }

            return value;
        }
    }
}
