using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection.Emit;
using UnityEngine;
namespace ImmersivePortals.Patches
{
    [HarmonyPatch(typeof(Player))]
    public static class PlayerPatch
    {
        private static bool hasTeleported;

        [HarmonyPatch("UpdateTeleport")]
        [HarmonyPrefix]
        public static void DecreaseTeleportTime(ref float dt, ref Player __instance, ref float ___m_teleportTimer)
        {
            if (__instance.m_teleporting && (!ImmersivePortals.enablePortalBlackScreen.Value || 
                ImmersivePortals.context.teleportStopwatch.Elapsed.TotalSeconds > Hud.instance.GetFadeDuration(__instance))) {

                dt *= ImmersivePortals.multiplyDeltaTimeBy.Value.Clamp(1, 10);

                if (___m_teleportTimer < 2f) {
                    ___m_teleportTimer = 2f; // Jump to first branch in UpdateTeleport skipping initial frames.
                }

                // Decrease artificial minimum teleport duration.
                ___m_teleportTimer *= (1f + ImmersivePortals.decreaseTeleportTimeByPercent.Value / 100f).Clamp(1f, 2f);
            }
        }

        [HarmonyPatch("UpdateTeleport")]
        [HarmonyPostfix]
        public static void LogTeleportTime(ref bool ___m_teleporting) {
            if (hasTeleported && !___m_teleporting) {
                hasTeleported = false;
                var time = ImmersivePortals.context.teleportStopwatch.Elapsed;
                var timeString = $"{time.ToString("s\\.fff", CultureInfo.InvariantCulture)} second{(time.TotalSeconds < 2 ? "" : "s")}";
                var distanceString = $"{Math.Truncate(TeleportWorldPatch.lastTeleportDistance)} meter{(TeleportWorldPatch.lastTeleportDistance < 2 ? "" : "s")}";
                DebugUtil.LogInfo("Teleported {0} in {1}", distanceString, timeString);
            }
        }

        [HarmonyPatch("UpdateTeleport")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> ApplyConsiderAreaLoadedConfig(IEnumerable<CodeInstruction> instructions)
        {
            return new CodeMatcher(instructions).MatchForward(false,
                        new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(ZNetScene), "IsAreaReady")))
                        .SetAndAdvance(OpCodes.Call, 
                                 Transpilers.EmitDelegate<Func<ZNetScene, Vector3, bool>>(IsAreaLoadedLazy).operand)
                        .InstructionEnumeration();
        }

        private static bool IsAreaLoadedLazy(ZNetScene zNetScene, Vector3 targetPos)
        {
            return ImmersivePortals.context.teleportStopwatch.Elapsed.TotalSeconds >
                   ImmersivePortals.considerSceneLoadedSeconds.Value || zNetScene.IsAreaReady(targetPos);
        }

        [HarmonyPatch("TeleportTo")]
        [HarmonyPostfix]
        public static void SetLastTeleportTime(bool __result)
        {
            if (__result) {
                ImmersivePortals.context.teleportStopwatch.Restart();
                hasTeleported = true;
            }
        }
    }
}
