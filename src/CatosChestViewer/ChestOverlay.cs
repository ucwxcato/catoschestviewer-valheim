using System;
using UnityEngine;

namespace CatosChestViewer
{
    internal static class ChestOverlay
    {
        private static Container _target;
        private static string _renderedText;
        private static string _fingerprint;
        private static float _nextReadTime;

        internal static void Apply(Hud hud, Player player)
        {
            if (!hud || !player || !ModConfig.Enabled.Value)
            {
                Clear();
                return;
            }

            if (!ChestTargetController.TryGetAccessibleChest(player, out Container target))
            {
                Clear();
                return;
            }

            float now = Time.unscaledTime;
            bool targetChanged = !ReferenceEquals(_target, target);
            if (targetChanged)
            {
                _target = target;
                _fingerprint = null;
                _renderedText = null;
                _nextReadTime = 0f;
            }

            if (targetChanged || now >= _nextReadTime)
            {
                _nextReadTime = now + Math.Max(0.025f, ModConfig.UpdateIntervalMs.Value / 1000f);
                if (!ChestInventoryReader.TryRead(target, player, out ChestContentsSnapshot snapshot))
                {
                    Clear();
                    return;
                }

                if (!string.Equals(_fingerprint, snapshot.Fingerprint, StringComparison.Ordinal))
                {
                    _fingerprint = snapshot.Fingerprint;
                    try
                    {
                        _renderedText = ChestTextFormatter.Format(target, snapshot);
                    }
                    catch (Exception ex)
                    {
                        Plugin.Log?.LogWarning($"Chest text formatting failed; hiding contents: {ex.Message}");
                        Clear();
                        return;
                    }
                }
            }

            if (hud.m_hoverName != null && !string.IsNullOrEmpty(_renderedText))
                hud.m_hoverName.text = _renderedText;
        }

        internal static void Clear()
        {
            _target = null;
            _fingerprint = null;
            _renderedText = null;
            _nextReadTime = 0f;
        }
    }
}
