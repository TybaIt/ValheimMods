using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
namespace ImmersivePortals.Patches
{
    [HarmonyPatch(typeof(Character))]
    public class CharacterPatch
    {
        [HarmonyPatch("ApplyDamage")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> PreventDamageWhileTeleporting(IEnumerable<CodeInstruction> instructions)
        {
            return new CodeMatcher(instructions).MatchForward(false,
                    new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(Character), "IsTeleporting")))
                .SetAndAdvance(OpCodes.Call, 
                    Transpilers.EmitDelegate<Func<Character, bool>>(player => 
                        ImmersivePortals.context.teleportStopwatch.Elapsed.TotalSeconds < 0.5f || player.IsTeleporting()).operand)
                .InstructionEnumeration();
        }
    }
}
