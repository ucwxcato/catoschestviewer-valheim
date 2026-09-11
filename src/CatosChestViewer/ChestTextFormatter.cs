using System;
using System.Text;
using System.Text.RegularExpressions;

namespace CatosChestViewer
{
    internal static class ChestTextFormatter
    {
        private static readonly Regex RichTextTag = new Regex("<[^>]*>", RegexOptions.Compiled);

        internal static string Format(Container container, ChestContentsSnapshot snapshot)
        {
            if (snapshot == null) return string.Empty;

            int maxLines = Math.Max(1, ModConfig.MaxLines.Value);
            int maxCharacters = Math.Max(100, ModConfig.MaxTextCharacters.Value);
            var lines = new StringBuilder();

            if (ModConfig.ShowHeader.Value)
                AppendLine(lines, Clean(ChestInventoryReader.Localize(container.GetHoverName())), maxCharacters);

            int itemLines = 0;
            foreach (ChestItemEntry item in snapshot.Items)
            {
                if (itemLines >= maxLines) break;
                string line = $"{Clean(item.Name)} x{Math.Max(0, item.Stack)}";
                if (!AppendLine(lines, line, maxCharacters)) break;
                itemLines++;
            }

            if (snapshot.Items.Count == 0 && ModConfig.ShowEmptyMessage.Value)
                AppendLine(lines, Clean(ModConfig.EmptyMessage.Value), maxCharacters);

            return lines.ToString().TrimEnd('\r', '\n');
        }

        private static bool AppendLine(StringBuilder text, string value, int maxCharacters)
        {
            value = string.IsNullOrWhiteSpace(value) ? "Unknown" : value.Trim();
            int required = value.Length + (text.Length == 0 ? 0 : Environment.NewLine.Length);
            if (text.Length + required > maxCharacters) return false;
            if (text.Length > 0) text.AppendLine();
            text.Append(value);
            return true;
        }

        private static string Clean(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            return RichTextTag.Replace(value, string.Empty)
                .Replace('\r', ' ')
                .Replace('\n', ' ')
                .Trim();
        }
    }
}
