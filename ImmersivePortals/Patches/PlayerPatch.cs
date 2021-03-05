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
        private static bool _prev_m_teleporting;

        [HarmonyPatch("IsTeleporting")]
        [HarmonyPrefix]
        public static bool DisableBlackScreen(bool __result)
        {
            return __result = ImmersivePortals.enablePortalBlackScreen.Value;
        }

        [HarmonyPatch("CanMove")]
        [HarmonyPrefix]
        public static void PreAllowMoving(ref bool ___m_teleporting, ref float ___m_teleportTimer)
        {
            _prev_m_teleporting = ___m_teleporting;
            if (___m_teleporting && ConsiderAreaLoaded())
            {
                ___m_teleporting = false;
            }
        }

        [HarmonyPatch("CanMove")]
        [HarmonyPostfix]
        public static void PostAllowMoving(ref bool ___m_teleporting)
        {
            ___m_teleporting = _prev_m_teleporting;
        }

        [HarmonyPatch(typeof(Player), "UpdateTeleport")]
        [HarmonyPostfix]
        public static void UpdateDeltaTime(ref float dt, ref float ___m_teleportTimer)
        {
            ___m_teleportTimer *= 1.5f; // Decreases the artificial upper bound a bit.
        }

        [HarmonyPatch(typeof(Player), "UpdateTeleport")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> LetMeInLetMeIn(IEnumerable<CodeInstruction> instructions)
        {
            return new CodeMatcher(instructions).MatchForward(false,
                        new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(ZNetScene), "IsAreaReady")))
                                .SetAndAdvance(OpCodes.Call, 
                                         Transpilers.EmitDelegate<Func<ZNetScene, Vector3, bool>>((zNetView, targetPos) => ConsiderAreaLoaded()).operand)
                                .InstructionEnumeration();
        }

        private static bool ConsiderAreaLoaded()
        {
            return DateTimeOffset.Now.Subtract(ImmersivePortals.context._lastTeleportTime).TotalSeconds >
                   ImmersivePortals.considerSceneLoadedSeconds.Value;
        }

        [HarmonyPatch(typeof(Player), "TeleportTo")]
        [HarmonyPostfix]
        public static void SetLastTeleportTime(ref Vector3 pos, ref Quaternion rot, ref bool distantTeleport, ref bool __result)
        {
            if (__result) {
                ImmersivePortals.context._lastTeleportTime = DateTime.Now;
            }
        }
    }
}
