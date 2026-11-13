using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TLKS.EditorTools
{
    public class TexturePost : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.Contains("/Resources/Textures/")) return;
            var ti = (TextureImporter)assetImporter;
            ti.maxTextureSize = 4096; ti.mipmapEnabled = true; ti.anisoLevel = 8; ti.textureCompression = TextureImporterCompression.CompressedHQ;
            ti.wrapMode = TextureWrapMode.Repeat; ti.sRGBTexture = true;
        }
    }

    public static class ProjectBuilder
    {
        static void SetInt(SerializedObject so, string prop, int v) { var p = so.FindProperty(prop); if (p != null) p.intValue = v; else Debug.LogWarning("missing prop " + prop); }
        static void SetBool(SerializedObject so, string prop, bool v) { var p = so.FindProperty(prop); if (p != null) p.boolValue = v; else Debug.LogWarning("missing prop " + prop); }
        static void SetFloat(SerializedObject so, string prop, float v) { var p = so.FindProperty(prop); if (p != null) p.floatValue = v; else Debug.LogWarning("missing prop " + prop); }

        [MenuItem("TLKS/Setup Project")]
        public static void Setup()
        {
            Directory.CreateDirectory("Assets/Settings"); Directory.CreateDirectory("Assets/Scenes");
            var rpPath = "Assets/Settings/TLKS_URP.asset"; var rdPath = "Assets/Settings/TLKS_Renderer.asset";
            var rp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(rpPath);
            if (rp == null)
            {
                var rd = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(rd, rdPath);
                rp = UniversalRenderPipelineAsset.Create(rd);
                AssetDatabase.CreateAsset(rp, rpPath);
            }
            var so = new SerializedObject(rp);
            SetBool(so, "m_SupportsHDR", true); SetInt(so, "m_MSAA", 4); SetFloat(so, "m_ShadowDistance", 220f); SetInt(so, "m_ShadowCascadeCount", 3);
            SetInt(so, "m_AdditionalLightsRenderingMode", 2); SetInt(so, "m_AdditionalLightsPerObjectLimit", 8); SetBool(so, "m_SoftShadowsSupported", true);
            SetInt(so, "m_MainLightShadowmapResolution", 4096); SetBool(so, "m_RequireDepthTexture", false);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(rp);
            GraphicsSettings.defaultRenderPipeline = rp;
            var cur = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++) { QualitySettings.SetQualityLevel(i, false); QualitySettings.renderPipeline = rp; }
            QualitySettings.SetQualityLevel(cur, false);

            // make sure the URP Lit shader (used for shadow casting via UsePass) is always included
            var gs = AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/GraphicsSettings.asset");
            var gso = new SerializedObject(gs);
            var arr = gso.FindProperty("m_AlwaysIncludedShaders");
            string[] need = { "Universal Render Pipeline/Lit", "Universal Render Pipeline/Unlit", "Hidden/Universal Render Pipeline/Blit" };
            foreach (var n in need)
            {
                var sh = Shader.Find(n); if (sh == null) continue; bool has = false;
                for (int i = 0; i < arr.arraySize; i++) if (arr.GetArrayElementAtIndex(i).objectReferenceValue == sh) has = true;
                if (!has) { arr.arraySize++; arr.GetArrayElementAtIndex(arr.arraySize - 1).objectReferenceValue = sh; }
            }
            gso.ApplyModifiedPropertiesWithoutUndo();

            PlayerSettings.productName = "The Last Known Signal"; PlayerSettings.companyName = "Space Apps Cairo 2026";
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.defaultScreenWidth = 1920; PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow; PlayerSettings.resizableWindow = true; PlayerSettings.runInBackground = true;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, true);
            var ps = AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/ProjectSettings.asset");
            var pso = new SerializedObject(ps); var ih = pso.FindProperty("activeInputHandler"); if (ih != null) { ih.intValue = 0; pso.ApplyModifiedPropertiesWithoutUndo(); }

            var sc = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(sc, "Assets/Scenes/Main.unity");
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/Main.unity", true) };
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            Debug.Log("TLKS setup complete.");
        }

        [MenuItem("TLKS/Build Windows")]
        public static void BuildWindows()
        {
            Setup();
            Directory.CreateDirectory("Builds");
            var opt = new BuildPlayerOptions { scenes = new[] { "Assets/Scenes/Main.unity" }, locationPathName = "Builds/TheLastKnownSignal/TheLastKnownSignal.exe", target = BuildTarget.StandaloneWindows64, options = BuildOptions.None };
            var rep = BuildPipeline.BuildPlayer(opt);
            Debug.Log("TLKS build result: " + rep.summary.result + " size " + rep.summary.totalSize);
            if (rep.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) EditorApplication.Exit(1);
        }
    }
}
