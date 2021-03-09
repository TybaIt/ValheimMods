using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
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
            VERSION = "0.2.7.2";

        internal readonly ManualLogSource log;
        internal readonly Harmony harmony;
        internal readonly Assembly assembly;
        public readonly string modFolder;

        #endregion

        public static string[] AssetBundles = new string[]
        {
            "Assets/AssetBundles/immersiveportals"
        };

        public static ImmersivePortals context;
        public static ConfigEntry<bool> enablePortalBlackScreen;
        public static ConfigEntry<bool> enableNotifications;
        public static ConfigEntry<double> considerSceneLoadedSeconds;
        public static ConfigEntry<int> decreaseTeleportTimeByPercent;
        public static ConfigEntry<int> multiplyDeltaTimeBy;
        public static ConfigEntry<int> nexusID;

        public ImmersivePortals() {
            log = Logger;
            harmony = new Harmony(GUID);
            assembly = typeof(ImmersivePortals).Assembly;
            modFolder = PathUtil.AssemblyDirectory;
        }

        private void Awake() {
            context = this;
            enablePortalBlackScreen = Config.Bind("General", "EnablePortalBlackScreen", true, "Enables the black transition screen when teleporting to distant portals outside the loaded area.");
            enableNotifications = Config.Bind("Debug", "DisplayMessageLogs", false, "Indicates if specific information should be shown as In-Game notifications in the top-left corner.");
            nexusID = Config.Bind("General", "NexusID", 268, "Nexus mod ID. Required for 'Nexus Update Check' (mod).");
            considerSceneLoadedSeconds = Config.Bind("TimeManipulation", "ConsiderAreaLoadedAfterSeconds", 3.75, "Indicates a threshold in seconds after which an area is considered substantially loaded so that the player can safely arrive and regain control.");
            decreaseTeleportTimeByPercent = Config.Bind("TimeManipulation", "DecreaseMinLoadTimeByPercent", 50,"Decreases the artificial minimum teleportation duration hardcoded by the developers (Iron Gate). 100% indicates removal of the minimum wait time and means that the only condition for arrival is the area load state. Value will be clamped in the range 0-100.");
            multiplyDeltaTimeBy = Config.Bind("TimeManipulation", "MultiplyDeltaTimeBy", 3,"Multiplies the delta time on each frame. Delta time decreases overall teleport time. Value will be clamped in the range 1-10.");
        }

        public void Start() {
            harmony.PatchAll(assembly);
        }
    }
}
