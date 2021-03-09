/*using HarmonyLib;
using ImmersivePortals.Entities;

namespace ImmersivePortals.Patches
{
    [HarmonyPatch(typeof(GameCamera))]
    public static class GameCameraPatch
    {
        [HarmonyPatch("UpdateCamera")]
        [HarmonyPrefix]
        public static void AddMainCameraForPreCulling(ref GameCamera __instance)
        {
            if (__instance.gameObject.GetComponent<MainCamera>() == null)
                __instance.gameObject.AddComponent<MainCamera>();
        }
    }
}*/
