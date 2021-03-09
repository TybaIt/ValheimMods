using HarmonyLib;
using ImmersivePortals.Entities;

namespace ImmersivePortals.Patches
{
    [HarmonyPatch(typeof(GameCamera))]
    public static class GameCameraPatch
    {
        [HarmonyPatch("Awake")]
        [HarmonyPostfix]
        public static void AddMainCameraForPreCulling(ref GameCamera __instance)
        {
            __instance.gameObject.AddComponent<MainCamera>();
        }
    }
}
