using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace MoneyRank.EditorTools
{
    /// <summary>
    /// Makes the V4 scene render in Player builds:
    ///  1) URP creates every renderer feature on startup; an INACTIVE Screen Space Ambient Occlusion feature has its
    ///     resources stripped from the build, which made UniversalRenderPipelineAsset.CreatePipeline throw on every frame.
    ///     The feature is not used by the V4 look, so it is removed from the renderer assets.
    ///  2) Shader.Find is unreliable in Players, so the runtime-built board uses material assets (their shaders are
    ///     referenced, hence included in the build).
    /// </summary>
    public static class V4RenderFix
    {
        private const string MaterialDir = "Assets/_MoneyRank/Resources/V4/Materials";

        [MenuItem("MoneyRank/V4/Fix Render Setup")]
        public static void Apply()
        {
            RemoveInactiveSsao("Assets/Settings/PC_Renderer.asset");
            RemoveInactiveSsao("Assets/Settings/Mobile_Renderer.asset");
            EnsureMaterial("V4_Lit", "Universal Render Pipeline/Lit");
            EnsureMaterial("V4_Unlit", "Universal Render Pipeline/Unlit");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[V4] Render setup fixed (SSAO feature removed when inactive; V4 material assets ensured).");
        }

        private static void RemoveInactiveSsao(string path)
        {
            var data = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(path);
            if (data == null) { Debug.LogWarning("[V4] Renderer asset not found: " + path); return; }
            var so = new SerializedObject(data);
            var features = so.FindProperty("m_RendererFeatures");
            var map = so.FindProperty("m_RendererFeatureMap");
            var removed = 0;
            for (var i = features.arraySize - 1; i >= 0; i--)
            {
                var feature = features.GetArrayElementAtIndex(i).objectReferenceValue as ScriptableRendererFeature;
                if (feature == null || !(feature is ScreenSpaceAmbientOcclusion) || feature.isActive) continue;
                features.GetArrayElementAtIndex(i).objectReferenceValue = null;
                features.DeleteArrayElementAtIndex(i);
                if (map != null && i < map.arraySize) map.DeleteArrayElementAtIndex(i);
                AssetDatabase.RemoveObjectFromAsset(feature);
                Object.DestroyImmediate(feature, true);
                removed++;
            }
            if (removed > 0)
            {
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(data);
            }
            Debug.Log("[V4] " + path + ": removed " + removed + " inactive SSAO feature(s).");
        }

        private static void EnsureMaterial(string name, string shaderName)
        {
            var path = MaterialDir + "/" + name + ".mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(path) != null) return;
            var shader = Shader.Find(shaderName);
            if (shader == null) { Debug.LogError("[V4] Shader not found: " + shaderName); return; }
            if (!AssetDatabase.IsValidFolder("Assets/_MoneyRank/Resources/V4/Materials"))
                AssetDatabase.CreateFolder("Assets/_MoneyRank/Resources/V4", "Materials");
            AssetDatabase.CreateAsset(new Material(shader), path);
        }
    }
}
