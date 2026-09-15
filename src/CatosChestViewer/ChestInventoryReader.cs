using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace CatosChestViewer
{
    internal sealed class ChestContentsSnapshot
    {
        internal ChestContentsSnapshot(List<ChestItemEntry> items, int occupiedSlots, int totalSlots, string fingerprint)
        {
            Items = items.AsReadOnly();
            OccupiedSlots = occupiedSlots;
            TotalSlots = totalSlots;
            Fingerprint = fingerprint;
        }

        internal IReadOnlyList<ChestItemEntry> Items { get; }
        internal int OccupiedSlots { get; }
        internal int TotalSlots { get; }
        internal string Fingerprint { get; }
    }

    internal sealed class ChestItemEntry
    {
        internal ChestItemEntry(string name, int stack, int sourceStackCount)
        {
            Name = name;
            Stack = stack;
            SourceStackCount = sourceStackCount;
        }

        internal string Name { get; }
        internal int Stack { get; }
        internal int SourceStackCount { get; }
    }

    internal static class ChestInventoryReader
    {
        private static Type _localizationType;
        private static PropertyInfo _localizationInstance;
        private static MethodInfo _localizeMethod;
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
                entries.Sort(CompareEntriesByName);

                int occupiedSlots = Math.Max(0, inventory.NrOfItems());
                int totalSlots = GetTotalSlots(inventory, occupiedSlots);
                snapshot = new ChestContentsSnapshot(entries, occupiedSlots, totalSlots, fingerprint);
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

            var aggregateByName = new Dictionary<string, AggregateEntry>(StringComparer.Ordinal);
            foreach (ItemDrop.ItemData item in items)
            {
                if (item == null || item.m_shared == null) continue;

                string name = item.m_shared.m_name ?? string.Empty;
                int stack = Math.Max(0, item.m_stack);
                if (!aggregateByName.TryGetValue(name, out AggregateEntry aggregate))
                {
                    aggregate = new AggregateEntry();
                    aggregateByName.Add(name, aggregate);
                }

                aggregate.Stack = (int)Math.Min((long)int.MaxValue, (long)aggregate.Stack + stack);
                aggregate.SourceStackCount++;
            }

            var fingerprint = new StringBuilder();
            var names = new List<string>(aggregateByName.Keys);
            names.Sort(StringComparer.Ordinal);
            foreach (string name in names)
            {
                AggregateEntry aggregate = aggregateByName[name];
                entries.Add(new ChestItemEntry(
                    Localize(name), aggregate.Stack, aggregate.SourceStackCount));
                fingerprint.Append(name).Append('\u001f')
                    .Append(aggregate.Stack).Append('\u001f')
                    .Append(aggregate.SourceStackCount).Append('\u001e');
            }

            return fingerprint.Length == 0 ? "empty" : fingerprint.ToString();
        }

        private sealed class AggregateEntry
        {
            internal int Stack;
            internal int SourceStackCount;
        }

        private static int CompareEntriesByName(ChestItemEntry left, ChestItemEntry right)
        {
            int localizedComparison = StringComparer.CurrentCultureIgnoreCase.Compare(left.Name, right.Name);
            return localizedComparison != 0
                ? localizedComparison
                : StringComparer.Ordinal.Compare(left.Name, right.Name);
        }

        private static int GetTotalSlots(Inventory inventory, int occupiedSlots)
        {
            int width = Math.Max(0, inventory.GetWidth());
            int height = Math.Max(0, inventory.GetHeight());
            long gridSlots = (long)width * height;

            // A compatible container should always expose a positive grid size.
            // Keep the displayed denominator meaningful if one reports an invalid
            // size while still containing stacks.
            return (int)Math.Min(int.MaxValue, Math.Max(gridSlots, (long)occupiedSlots));
        }

        internal static string Localize(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "Unknown item";

            try
            {
                EnsureLocalizationApi();
                object localization = _localizationInstance?.GetValue(null, null);
                if (localization != null && _localizeMethod != null)
                {
                    string localized = _localizeMethod.Invoke(localization, new object[] { value }) as string;
                    if (!string.IsNullOrWhiteSpace(localized) && localized != value)
                        return localized;
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"Item localization failed: {ex.Message}");
            }

            return HumanizeKey(value);
        }

        private static void EnsureLocalizationApi()
        {
            if (_localizationType != null) return;

            // Localization is defined by assembly_guiutils, not
            // assembly_valheim. Resolve it lazily because guiutils may not be
            // loaded yet when the plugin's Awake method runs.
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType("Localization", false);
                if (type == null) continue;

                _localizationType = type;
                _localizationInstance = type.GetProperty(
                    "instance", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                _localizeMethod = type.GetMethod(
                    "Localize", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                    null, new[] { typeof(string) }, null);
                return;
            }
        }

        private static string HumanizeKey(string value)
        {
            string key = value.Trim();
            if (key.StartsWith("$", StringComparison.Ordinal)) key = key.Substring(1);
            if (key.StartsWith("item_", StringComparison.OrdinalIgnoreCase)) key = key.Substring(5);
            if (key.StartsWith("piece_", StringComparison.OrdinalIgnoreCase)) key = key.Substring(6);
            key = key.Replace('_', ' ').Trim();
            if (key.Length == 0) return "Unknown item";
            return char.ToUpperInvariant(key[0]) + key.Substring(1);
        }
    }
}
