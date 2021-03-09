using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace ImmersivePortals
{
    public static class AssetUtil
    {
        private static readonly IReadOnlyList<AssetBundle> _assetBundles;
        static AssetUtil()
        {
            var assetBundles = new List<AssetBundle>();
            foreach (var bundle in ImmersivePortals.AssetBundles)
            {
                var assetBundle = AssetBundle.LoadFromFile(Path.Combine(PathUtil.AssemblyDirectory, bundle));
                if (assetBundle == null) {
                    DebugUtil.LogError("Failed to load AssetBundle '{0}'", bundle);
                    continue;
                }
                assetBundles.Add(assetBundle);
                DebugUtil.LogInfo("╔ Loaded AssetBundle '{0}'", assetBundle.name);
                var assets = assetBundle.GetAllAssetNames();
                for (int i = 0; i < assets.Length; i++) {
                    DebugUtil.LogInfo("{0}══ '{1}'", i == assets.Length - 1 ? "╚" : "╠", assets[i]);
                }
            }
            _assetBundles = assetBundles;
        }

        public static T LoadAsset<T>(string assetPath)
        {
            object asset = null;
            foreach (var bundle in _assetBundles)
            {
                asset = bundle.LoadAsset(assetPath, typeof(T));
                if (asset != null) break;
            }

            if (asset == null) {
                DebugUtil.LogWarning("Failed to load asset '{0}'", assetPath);
                return default;
            }

            try {
                return (T)Convert.ChangeType(asset, typeof(T));
            } 
            catch (InvalidCastException) {
                DebugUtil.LogError("Invalid cast of asset '{0}' to type '{1}'", assetPath, typeof(T).ToString());
                return default;
            }
        }
    }
}
