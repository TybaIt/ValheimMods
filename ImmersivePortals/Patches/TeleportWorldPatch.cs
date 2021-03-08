using HarmonyLib;
using ImmersivePortals.Utils;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using ImmersivePortals.Entities;
using Steamworks;
using UnityEngine;

namespace ImmersivePortals.Patches
{
    [HarmonyPatch(typeof(TeleportWorld))]
    public static class TeleportWorldPatch {

        //[HarmonyPatch("TargetFound")] 
        //[HarmonyPostfix]
        public static void CreateRenderTarget(TeleportWorld __instance, bool __result)
        {
            var go = __instance.gameObject;

            PortalCamera portalCamera;

            if (!__result) {
                if (go.TryGetComponent(out portalCamera))
                    UnityEngine.Object.Destroy(portalCamera);
                return;
            }

            if (go.TryGetComponent(out portalCamera)) {
                return;
            }

            ZDOID zdoid = __instance.m_nview.GetZDO().GetZDOID("target");
            if (zdoid == ZDOID.None) {
                return;
            }
            ZDO zdo = ZDOMan.instance.GetZDO(zdoid);

            if (!ZoneSystem.instance.IsZoneLoaded(zdo.GetPosition()))
            {
                return;
            }


            //TODO: Implement render view.
            var entry = __instance.GetComponentInChildren<TeleportWorldTrigger>();

            //TODO circle shader for plane (Cylinder primitive is bad due to both sides being rendered)
            GameObject renderTarget = GameObject.CreatePrimitive(PrimitiveType.Plane);
            Material mat = new Material(AssetUtil.LoadAsset<Shader>("assets/screencutoutshader.shader"));
            renderTarget.GetComponent<Renderer>().material = mat;
            renderTarget.transform.position = new Vector3(entry.transform.position.x, entry.transform.position.y + 0.5f, entry.transform.position.z);
            renderTarget.transform.Rotate(__instance.transform.rotation.eulerAngles.x,__instance.transform.rotation.eulerAngles.y - 90f,__instance.transform.rotation.eulerAngles.z - 90f, Space.Self);
            renderTarget.transform.localScale = new Vector3(0.2f,0.2f,1f); //1.742178
            //renderTarget.transform.position = new Vector3(entry.transform.position.x, entry.transform.position.y + 0.5f, entry.transform.position.z);
            //renderTarget.transform.Rotate(__instance.transform.rotation.eulerAngles.x,__instance.transform.rotation.eulerAngles.y - 90f,__instance.transform.rotation.eulerAngles.z - 90f, Space.Self);
            //renderTarget.transform.localScale = new Vector3(entry.transform.localScale.x + 0.9f, 0.05f, entry.transform.localScale.z + 0.9f); //1.742178
            renderTarget.GetComponent<Collider>().enabled = false;

            go.AddComponent<PortalCamera>();
            portalCamera = go.GetComponent<PortalCamera>();
            portalCamera.gameObject.AddComponent<Camera>();
            var camera = portalCamera.gameObject.GetComponent<Camera>();

            camera.transform.position =
                new Vector3(zdo.GetPosition().x, zdo.GetPosition().y + 0.5f,
                    zdo.GetPosition().z); //new Vector3(zdo.GetPosition().x, zdo.GetPosition().y + 0.5f, zdo.GetPosition().z);
            camera.transform.rotation = zdo.GetRotation();
            if (camera.targetTexture != null)
                camera.targetTexture.Release();
            camera.targetTexture = new RenderTexture(Screen.width, Screen.height, 24);
            mat.mainTexture = camera.targetTexture;
            portalCamera.camera = camera;
            portalCamera.otherPortal = new Transform() {position = zdo.GetPosition(), rotation = zdo.GetRotation()};
            portalCamera.playerCamera = GameCamera.instance.transform;
            portalCamera.portal = __instance.transform;
            portalCamera.renderTarget = renderTarget;
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
            Vector3 a = targetTrigger.transform.rotation * Vector3.forward * 1.15f;
            var targetPos = targetTrigger.transform.position + a * instance.m_exitDistance + positionOffset;

            if (!ZNetScene.instance.OutsideActiveArea(targetPos, player.transform.position)/*|| target.gameObject.activeSelf*/) {
                // Portal is in the active area thus close enough for instant teleport.

                if (player.IsTeleporting() || player.m_teleportCooldown < 0.1f ) {
                    return false;
                }

                // Teleport him!
                PlayerPatch._lastTeleportTime = DateTime.Now;
                player.m_teleporting = true; // May act as a lock for async routines.
                player.m_maxAirAltitude = player.transform.position.y;
                targetPos.y += 0.2f;
                player.transform.position = targetPos;
                player.transform.rotation = targetRot;
                player.m_body.velocity = Vector3.zero;
                player.SetLookDir(targetRot * Vector3.forward);
                player.m_teleportCooldown = 0f;
                player.m_teleporting = false;
                player.ResetCloth();
                return true;
            }
            return player.TeleportTo(targetPos, targetRot, distantTeleport);
        }
    }
}
