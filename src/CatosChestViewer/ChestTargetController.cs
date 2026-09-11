using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace CatosChestViewer
{
    internal static class ChestTargetController
    {
        private static readonly MethodInfo CheckAccessMethod = AccessTools.Method(
            typeof(Container), "CheckAccess", new[] { typeof(long) });

        private static bool _accessMethodWarningLogged;
        private static float _nextAccessWarningTime;

        internal static bool TryGetAccessibleChest(Player player, out Container container)
        {
            container = null;
            if (!player || !ModConfig.Enabled.Value) return false;

            GameObject hoverObject = player.GetHoverObject();
            if (!hoverObject) return false;

            Container candidate = hoverObject.GetComponentInParent<Container>();
            if (!candidate || !IsAccessible(candidate)) return false;

            container = candidate;
            return true;
        }

        private static bool IsAccessible(Container container)
        {
            try
            {
                if (container.m_checkGuardStone &&
                    !PrivateArea.CheckAccess(container.transform.position, 0f, false, false))
                    return false;

                if (CheckAccessMethod == null)
                {
                    if (!_accessMethodWarningLogged)
                    {
                        _accessMethodWarningLogged = true;
                        Plugin.Log?.LogError("Container.CheckAccess(long) was not found; hiding chest contents.");
                    }
                    return false;
                }

                Game game = Game.instance;
                if (!game) return false;
                PlayerProfile profile = game.GetPlayerProfile();
                if (profile == null) return false;

                object result = CheckAccessMethod.Invoke(container, new object[] { profile.GetPlayerID() });
                return result is bool allowed && allowed;
            }
            catch (Exception ex)
            {
                if (Time.unscaledTime >= _nextAccessWarningTime)
                {
                    _nextAccessWarningTime = Time.unscaledTime + 2f;
                    Plugin.Log?.LogWarning($"Chest access check failed; hiding contents: {ex.Message}");
                }
                return false;
            }
        }
    }
}
