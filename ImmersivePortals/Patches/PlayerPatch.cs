using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;

namespace ImmersivePortals.Patches
{
    [HarmonyPatch(typeof(Player))]
    public static class PlayerPatch
    {
        private static DateTime _lastTeleportTime = DateTime.Now;

        [HarmonyPatch("CanMove")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> ReplaceCanMoveConditional(IEnumerable<CodeInstruction> instructions)
        {
            return new CodeMatcher(instructions).MatchForward(false,
                    new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(Player), "m_teleporting")))
                    .SetAndAdvance(OpCodes.Call, Transpilers.EmitDelegate<Func<Player, bool>>(player => !IsAreaLoadedLazy()).operand)
                    .InstructionEnumeration();
        }

        [HarmonyPatch(typeof(Player), "UpdateTeleport")]
        [HarmonyPostfix]
        public static void DecreaseTeleportTime(ref float dt, ref Player __instance, ref float ___m_teleportTimer)
        {
            if (!ImmersivePortals.enablePortalBlackScreen.Value ||
                DateTimeOffset.Now.Subtract(_lastTeleportTime).TotalSeconds > Hud.instance.GetFadeDuration(__instance)) {
                // Decreases the artificial minimum teleport duration.
                ___m_teleportTimer *= (1 + (float)ImmersivePortals.decreaseTeleportTimeByPercent.Value / 100).Clamp(1f, 2f);
            }
        }

        [HarmonyPatch(typeof(Player), "UpdateTeleport")]
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

        [HarmonyPatch(typeof(Player), "TeleportTo")]
        [HarmonyPostfix]
        public static void SetLastTeleportTime(ref Vector3 pos, ref Quaternion rot, ref bool distantTeleport, ref bool __result)
        {
            if (__result) {
                _lastTeleportTime = DateTime.Now;
            }
        }
    }
}
