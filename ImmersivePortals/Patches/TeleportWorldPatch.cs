using HarmonyLib;
using ImmersivePortals.Utils;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;

namespace ImmersivePortals.Patches
{
    [HarmonyPatch(typeof(TeleportWorld))]
    public static class TeleportWorldPatch {

        //[HarmonyPatch("Awake")]
        //[HarmonyPostfix]
        public static void CreateRenderTarget(TeleportWorld __instance, ref ZNetView ___m_nview, ref bool ___m_model)
        {
            //TODO: Implement render view.
            ZDOID zdoid = ___m_nview.GetZDO().GetZDOID("target");
            if (zdoid == ZDOID.None)
            {
                return;
            }
            ZDO zdo = ZDOMan.instance.GetZDO(zdoid);

            var entry = __instance.GetComponentInChildren<TeleportWorldTrigger>();

            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            
            Material mat = new Material(AssetUtil.GetScreenCutoutShader());
            var tex = new RenderTexture();
            mat.mainTexture = tex;
            
            cube.gameObject.GetComponent<MeshRenderer>().material = mat;
            cube.transform.position = entry.transform.position;
            cube.transform.rotation = entry.transform.rotation;
            cube.transform.localScale = new Vector3(entry.transform.localScale.x,entry.transform.localScale.y,0.1f);//1.742178
            cube.GetComponent<Collider>().enabled = false;

            Camera camera = new Camera();
            camera.transform.position = zdo.GetPosition();
            camera.transform.rotation = zdo.GetRotation();
            if (camera.targetTexture != null) 
                camera.targetTexture.Release();
            camera.targetTexture = new RenderTexture(Screen.width, Screen.height, 24);
            mat.mainTexture = camera.targetTexture;
        }

        [HarmonyPatch("Teleport")]
        [HarmonyPrefix]
        public static bool IsFront(ref Player player, TeleportWorld __instance, ref ZNetView ___m_nview, ref float ___m_exitDistance)
        {
            var trigger = __instance.GetComponentInChildren<TeleportWorldTrigger>();
            Vector3 triggerToPlayer = player.transform.position - trigger.transform.position;
            float dotProduct = Vector3.Dot(trigger.transform.forward - trigger.transform.up, triggerToPlayer);
            // Player has moved across the portal from front side.
            return dotProduct > 1f;
        }

        [HarmonyPatch("Teleport")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> ReplaceTeleportToWithImmersiveApproach(IEnumerable<CodeInstruction> instructions)
        {
            return new CodeMatcher(instructions).MatchForward(false,
                    new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(Character), "TeleportTo")))
                    .InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_0))
                    .InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_0))
                    .SetAndAdvance(OpCodes.Call, 
                        Transpilers.EmitDelegate<Func<Character, Vector3, Quaternion, bool, TeleportWorld, ZDOID, bool>>(ImmersiveTeleportTo).operand)
                    .InstructionEnumeration();
        }

        public static bool ImmersiveTeleportTo(Character c, Vector3 pos, Quaternion rot, bool distantTeleport, TeleportWorld instance, ZDOID zdoid)
        {
            var player = (Player)c;
            var zdo = ZDOMan.instance.GetZDO(zdoid);
            var trigger = instance.GetComponentInChildren<TeleportWorldTrigger>();

            var target = ZNetScene.instance.FindInstance(zdo)?.gameObject;

            if (target == null) {
                // Fallback to original method.
                return player.TeleportTo(pos, rot, distantTeleport);
            }

            // Calculate angle at target trigger bounds that reflects angle at which overlapping at source trigger occurred.
            var targetTrigger = target.GetComponentInChildren<TeleportWorldTrigger>();

            float rotationDiff = -Quaternion.Angle(trigger.transform.rotation, targetTrigger.transform.rotation);
            var targetRot = targetTrigger.transform.rotation;

            Vector3 triggerToPlayer = player.transform.position - trigger.transform.position;
            Vector3 positionOffset = Quaternion.Euler(0f, rotationDiff, 0f) * triggerToPlayer;
            Vector3 a = targetTrigger.transform.rotation * Vector3.forward * 1.1f;
            var targetPos = targetTrigger.transform.position + a * instance.m_exitDistance + positionOffset;

            if (!ZNetScene.instance.OutsideActiveArea(targetPos)) {
                // Portal is in the active area thus close enough for instant teleport.

                if (player.IsTeleporting()/*|| player.m_teleportCooldown < 2f */) {
                    return false;
                }

                // Teleport him!
                player.m_teleporting = true; // May act as a lock for async routines.
                targetPos.y = ZoneSystem.instance.GetSolidHeight(targetPos) + 0.5f;
                player.transform.position = targetPos;
                player.transform.rotation = targetRot;
                player.m_body.velocity = Vector3.zero;
                player.m_maxAirAltitude = player.transform.position.y;
                player.SetLookDir(targetRot * Vector3.forward);
                //player.m_teleportCooldown = 0f;
                player.m_teleporting = false;
                player.ResetCloth();
                return true;
            }
            return player.TeleportTo(targetPos, targetRot, true);
        }
    }
}
