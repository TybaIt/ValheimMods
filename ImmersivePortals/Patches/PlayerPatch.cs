using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection.Emit;
using ImmersivePortals.Entities;
using UnityEngine;

namespace ImmersivePortals.Patches
{
    [HarmonyPatch(typeof(Player))]
    public static class PlayerPatch
    {
        internal static DateTime _lastTeleportTime = DateTime.Now;
        private static bool hasTeleported;

        [HarmonyPatch("UpdateTeleport")]
        [HarmonyPrefix]
        public static void DecreaseTeleportTime(ref float dt, ref Player __instance, ref float ___m_teleportTimer)
        {
            if (__instance.m_teleporting && (!ImmersivePortals.enablePortalBlackScreen.Value || 
                 DateTimeOffset.Now.Subtract(_lastTeleportTime).TotalSeconds > Hud.instance.GetFadeDuration(__instance))) {

                dt *= ImmersivePortals.multiplyDeltaTimeBy.Value.Clamp(1, 10);
                // Decreases the artificial minimum teleport duration.
                ___m_teleportTimer *= (1f + ImmersivePortals.decreaseTeleportTimeByPercent.Value / 100f).Clamp(1f, 2f);
                
            }
        }

        [HarmonyPatch("UpdateTeleport")]
        [HarmonyPostfix]
        public static void LogTeleportTime(ref bool ___m_teleporting) {
            if (hasTeleported && !___m_teleporting) {
                hasTeleported = false;
                var time = DateTimeOffset.Now.Subtract(_lastTeleportTime);
                var timeString = $"{time.ToString("s\\.fff", CultureInfo.InvariantCulture)} second{(time.TotalSeconds < 2 ? "" : "s")}";
                DebugUtil.LogInfo("Teleport took {0}", timeString);
            }
        }

        [HarmonyPatch("UpdateTeleport")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> ApplyConsiderAreaLoadedConfig(IEnumerable<CodeInstruction> instructions)
        {
            return new CodeMatcher(instructions).MatchForward(false,
                        new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(ZNetScene), "IsAreaReady")))
                        .SetAndAdvance(OpCodes.Call, 
                                 Transpilers.EmitDelegate<Func<ZNetScene, Vector3, bool>>((zNetView, targetPos) => IsAreaLoadedLazy()).operand)
                        .InstructionEnumeration();
        }

        private static bool IsAreaLoadedLazy()
        {
            return DateTimeOffset.Now.Subtract(_lastTeleportTime).TotalSeconds >
                   ImmersivePortals.considerSceneLoadedSeconds.Value;
        }

        [HarmonyPatch("TeleportTo")]
        [HarmonyPostfix]
        public static void SetLastTeleportTime(bool __result)
        {
            if (__result) {
                _lastTeleportTime = DateTime.Now;
                hasTeleported = true;
            }
        }
    }
}
