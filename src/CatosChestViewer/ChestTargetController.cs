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

            // Valheim stores the collider and Container on different prefab
            // levels depending on the container type. Search both directions
            // around the native hover object.
            Container candidate = hoverObject.GetComponentInParent<Container>()
                ?? hoverObject.GetComponentInChildren<Container>(true);
            if (!candidate || !IsAccessible(candidate, player.GetPlayerID())) return false;

            container = candidate;
            return true;
        }

        private static bool IsAccessible(Container container, long playerId)
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

                object result = CheckAccessMethod.Invoke(container, new object[] { playerId });
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
