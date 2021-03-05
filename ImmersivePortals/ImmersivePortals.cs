using System;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.IO;
using System.Reflection;
using BepInEx.Configuration;
using ImmersivePortals.Utils;
using UnityEngine;

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
            VERSION = "0.2.5";

        internal readonly ManualLogSource log;
        internal readonly Harmony harmony;
        internal readonly Assembly assembly;
        public readonly string modFolder;

        public DateTime _lastTeleportTime { get; internal set; }

        #endregion

        public static ImmersivePortals context;
        public static ConfigEntry<bool> enablePortalBlackScreen;
        public static ConfigEntry<double> considerSceneLoadedSeconds;
        public static ConfigEntry<bool> forceInstantiatePortalExit;
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
            enablePortalBlackScreen = Config.Bind("General", "EnablePortalBlackScreen", false, "Enables the black transition screen when teleporting to distant portals outside the loaded area.");
            considerSceneLoadedSeconds = Config.Bind("General", "ConsiderAreaLoadedAfterSeconds", 4.5, "Indicates a threshold in seconds after which an area is considered substantially loaded so that the player can safely arrive and regain control.");
            nexusID = Config.Bind("General", "NexusID", 268, "Nexus mod ID. Required for update checks.");
            forceInstantiatePortalExit = Config.Bind("RiskArea", "ForceInstantiateExitPortal", false, "Enables instant travel for distant exit portals outside the active area. Collision logic may not be loaded on arrival. Enable at your own risk.");
            
        }

        public void Start()
        {
            harmony.PatchAll(assembly);
        }
    }
}
