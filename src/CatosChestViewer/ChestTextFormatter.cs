using System;
using System.Text;
using System.Text.RegularExpressions;

namespace CatosChestViewer
{
    internal static class ChestTextFormatter
    {
        private static readonly Regex RichTextTag = new Regex("<[^>]*>", RegexOptions.Compiled);
        private const string HeaderColor = "#FFD166FF";
        private const string NameColor = "#F2F5FFFF";
        private const string CountColor = "#7CFFB2FF";

        internal static string Format(Container container, ChestContentsSnapshot snapshot)
        {
            if (snapshot == null) return string.Empty;

            int maxLines = Math.Max(1, ModConfig.MaxLines.Value);
            int maxCharacters = Math.Max(100, ModConfig.MaxTextCharacters.Value);
            var lines = new StringBuilder();

            if (ModConfig.ShowHeader.Value)
            {
                string header = Clean(ChestInventoryReader.Localize(container.GetHoverName()));
                AppendLine(lines, $"<color={HeaderColor}><b>{EscapeRichText(header)}</b></color>", maxCharacters);
            }

            int itemLines = 0;
            foreach (ChestItemEntry item in snapshot.Items)
            {
                if (itemLines >= maxLines) break;
                string itemName = EscapeRichText(Clean(item.Name));
                string line = $"<color={NameColor}>{itemName}</color> <color={CountColor}><b>x{Math.Max(0, item.Stack)}</b></color>";
                if (!AppendLine(lines, line, maxCharacters)) break;
                itemLines++;
            }

            if (snapshot.Items.Count == 0 && ModConfig.ShowEmptyMessage.Value)
                AppendLine(lines, $"<color={CountColor}>{EscapeRichText(Clean(ModConfig.EmptyMessage.Value))}</color>", maxCharacters);

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

        private static string EscapeRichText(string value)
        {
            return value.Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;");
        }
    }
}
