using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using ImmersivePortals.Utils;
using System.Reflection;

namespace ImmersivePortals
{
    [BepInPlugin(GUID, MODNAME, VERSION)]
    public class ImmersivePortals : BaseUnityPlugin
    {
        #region[Declarations]

        public const string
            MODNAME = "ImmersivePortals",
            AUTHOR = "Nekres",
            GUID = AUTHOR + "_" + MODNAME,
            VERSION = "0.2.6.3";

        internal readonly ManualLogSource log;
        internal readonly Harmony harmony;
        internal readonly Assembly assembly;
        public readonly string modFolder;

        #endregion

        public static ImmersivePortals context;
        public static ConfigEntry<bool> enablePortalBlackScreen;
        public static ConfigEntry<double> considerSceneLoadedSeconds;
        public static ConfigEntry<bool> forceInstantiatePortalExit;
        public static ConfigEntry<int> decreaseTeleportTimeByPercent;
        public static ConfigEntry<int> nexusID;

        public ImmersivePortals() {
            log = Logger;
            harmony = new Harmony(GUID);
            assembly = typeof(ImmersivePortals).Assembly;
            modFolder = PathUtil.AssemblyDirectory;
        }

        private void Awake()
        {
            context = this;
            enablePortalBlackScreen = Config.Bind("General", "EnablePortalBlackScreen", true, "Enables the black transition screen when teleporting to distant portals outside the loaded area.");
            considerSceneLoadedSeconds = Config.Bind("General", "ConsiderAreaLoadedAfterSeconds", 3.75, "Indicates a threshold in seconds after which an area is considered substantially loaded so that the player can safely arrive and regain control.");
            nexusID = Config.Bind("General", "NexusID", 268, "Nexus mod ID. Required for 'Nexus Update Check' (mod).");
            forceInstantiatePortalExit = Config.Bind("Miscellaneous", "ForceInstantiateExitPortal", true, "Enforces immersive approach for distant portals outside the active area. Falls back to original logic if it fails to instantiate the exit portal.");
            decreaseTeleportTimeByPercent = Config.Bind("RiskArea", "DecreaseTeleportTimeByPercent", 50,"Decreases the artificial minimum teleportation duration hardcoded by the developers (Iron Gate). 100% indicates removal of the minimum wait time and means that the only condition for arrival is the area load state.");
        }

        public void Start()
        {
            harmony.PatchAll(assembly);
        }
    }
}
