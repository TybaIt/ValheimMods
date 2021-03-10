using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
namespace ImmersivePortals.Patches
{
    [HarmonyPatch(typeof(Hud))]
    public static class HudPatch
    {
        [HarmonyPatch("UpdateBlackScreen")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> ApplyPortalBlackScreenConfig(IEnumerable<CodeInstruction> instructions)
        {
            return new CodeMatcher(instructions).MatchForward(false,
                    new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(Character), "IsTeleporting")))
                .SetAndAdvance(OpCodes.Call, 
                    Transpilers.EmitDelegate<Func<Player, bool>>(player => ImmersivePortals.enablePortalBlackScreen.Value && player.IsTeleporting()).operand)
                .InstructionEnumeration();
        }
    }
}
