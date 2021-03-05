using System.IO;
using UnityEngine;

namespace ImmersivePortals.Utils
{
    public static class AssetUtil
    {
        private static readonly AssetBundle shaderAsset; 
        static AssetUtil()
        {
            shaderAsset = AssetBundle.LoadFromFile(Path.Combine(PathUtil.AssemblyDirectory, "Shaders\\screencutoutshader"));
        }
        public static Shader GetScreenCutoutShader()
        {
            if (shaderAsset == null) {
                Debug.Log($"[{ImmersivePortals.MODNAME}] Failed to load AssetBundle");
            }
            return shaderAsset?.LoadAsset<Shader>("ScreenCutoutShader.shader") ?? Shader.Find("Standard");
        }
    }
}
