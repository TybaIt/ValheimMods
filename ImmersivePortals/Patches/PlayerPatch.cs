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
        [HarmonyPatch("CanMove")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> CanMoveEarly(IEnumerable<CodeInstruction> instructions)
        {
            return new CodeMatcher(instructions).MatchForward(false,
                    new CodeMatch(OpCodes.Ldfld, AccessTools.PropertyGetter(typeof(Player), "m_teleporting")))
                .SetAndAdvance(OpCodes.Call, 
                    Transpilers.EmitDelegate<Func<Player, bool>>(player => !IsAreaLoadedLazy()).operand)
                .InstructionEnumeration();
        }

        [HarmonyPatch(typeof(Player), "UpdateTeleport")]
        [HarmonyPostfix]
        public static void UpdateDeltaTime(ref float dt, ref Player __instance, ref float ___m_teleportTimer)
        {
            if (!ImmersivePortals.enablePortalBlackScreen.Value ||
                DateTimeOffset.Now.Subtract(ImmersivePortals.context._lastTeleportTime).TotalSeconds > Hud.instance.GetFadeDuration(__instance)) {
                ___m_teleportTimer *= 1.5f; // Decreases the artificial upper bound by 50%.
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
