using System.Linq;
using System.Reflection;
using MoneyRank.V4.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace MoneyRank.EditorTools
{
    /// <summary>Creates the V4 scene (one bootstrap object; everything else is built at runtime from the CSV content).</summary>
    public static class V4SceneBuilder
    {
        public const string ScenePath = "Assets/_MoneyRank/Scenes/MoneyRankV4.unity";

        [MenuItem("MoneyRank/V4/Create Scene")]
        public static void CreateScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var go = new GameObject("MoneyRankV4");
            go.AddComponent<V4Bootstrap>();
            var cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.05f, 0.09f, 0.15f);
                cam.transform.position = new Vector3(0f, 12f, -10f);
                cam.transform.rotation = Quaternion.Euler(52f, 0f, 0f);
            }
            EditorSceneManager.SaveScene(scene, ScenePath);

            var list = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
            list.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = list.ToArray();

            EnsureAlwaysIncluded("Universal Render Pipeline/Lit");
            EnsureAlwaysIncluded("Universal Render Pipeline/Unlit");
            EnsureAlwaysIncluded("Sprites/Default");
            AssetDatabase.SaveAssets();
            Debug.Log("[V4] Scene created at " + ScenePath + " and set as the first build scene.");
        }

        private static void EnsureAlwaysIncluded(string shaderName)
        {
            var shader = Shader.Find(shaderName);
            if (shader == null) { Debug.LogWarning("[V4] Shader not found: " + shaderName); return; }
            var getter = typeof(GraphicsSettings).GetMethod("GetGraphicsSettings", BindingFlags.NonPublic | BindingFlags.Static);
            if (getter == null) return;
            var so = new SerializedObject((Object)getter.Invoke(null, null));
            var array = so.FindProperty("m_AlwaysIncludedShaders");
            for (var i = 0; i < array.arraySize; i++)
                if (array.GetArrayElementAtIndex(i).objectReferenceValue == shader) return;
            array.arraySize++;
            array.GetArrayElementAtIndex(array.arraySize - 1).objectReferenceValue = shader;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
