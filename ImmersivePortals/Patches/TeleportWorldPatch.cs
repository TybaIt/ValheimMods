using System;
using HarmonyLib;
using ImmersivePortals.Utils;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using ImmersivePortals.Entities;
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
        public static bool TeleportFast(ref Player player, TeleportWorld __instance, ref ZNetView ___m_nview, ref float ___m_exitDistance)
        {
            var entry = __instance.GetComponentInChildren<TeleportWorldTrigger>();

            Vector3 portalToPlayer = player.transform.position - entry.transform.position;
            float dotProduct = Vector3.Dot(entry.transform.forward - entry.transform.up, portalToPlayer);

            if (dotProduct < 1f) {
                // Player has moved across the portal from the wrong side.
                return false;
            }

            #region original game code TeleportWorld.Teleport
            // TODO: Could be replaced by Transpiler which disables .Teleport call. Rest of the body needs to be inserted.
            if (!__instance.TargetFound())
            {
                return false;
            }
            if (!player.IsTeleportable())
            {
                player.Message(MessageHud.MessageType.Center, "$msg_noteleport", 0, null);
                return false;
            }
            ZDOID zdoid = ___m_nview.GetZDO().GetZDOID("target");
            if (zdoid == ZDOID.None)
            {
                return false;
            }
            ZDO zdo = ZDOMan.instance.GetZDO(zdoid);
            #endregion

            var exitPortal = ZNetScene.instance.FindInstance(zdo)?.gameObject ?? ImmersivePortals.forceInstantiatePortalExit.Value ? 
                                     ZNetScene.instance.CreateObject(zdo) : null;

            if (exitPortal == null)
            {
                if (ImmersivePortals.forceInstantiatePortalExit.Value)
                    ZLog.Log($"[{ImmersivePortals.MODNAME}] Unable to create exit portal -> Fallback to original method");
                return true;
            }

            ImmersivePortals.context._lastTeleportTime = DateTime.Now;

            var exit = exitPortal.GetComponentInChildren<TeleportWorldTrigger>();

            ZLog.Log($"Teleporting {player.GetPlayerName()}.");
            player.m_teleporting = true;
            float rotationDiff = -Quaternion.Angle(entry.transform.rotation, exit.transform.rotation);
            //rotationDiff += 180;
            //player.transform.Rotate(Vector3.up, rotationDiff);
            var targetRot = exit.transform.rotation;

            Vector3 positionOffset = Quaternion.Euler(0f, rotationDiff, 0f) * portalToPlayer;
            Vector3 a = exit.transform.rotation * Vector3.forward * 1.1f;
            var targetPos = exit.transform.position + a * ___m_exitDistance + positionOffset;
            targetPos.y = ZoneSystem.instance.GetSolidHeight(targetPos) + 0.5f;

            // Teleport him!
            player.transform.position = targetPos;
            player.transform.rotation = targetRot;
            player.m_body.velocity = Vector3.zero;
            player.m_maxAirAltitude = player.transform.position.y;
            player.SetLookDir(targetRot * Vector3.forward);
            player.m_teleporting = false;
            player.ResetCloth();
            return false;
        }

    }
}
