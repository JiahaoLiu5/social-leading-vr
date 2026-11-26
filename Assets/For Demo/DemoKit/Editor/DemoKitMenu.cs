using DemoKit.Sample;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DemoKit.Editor
{
    /// <summary>Menu entries of the kit.</summary>
    public static class DemoKitMenu
    {
        /// <summary>Opens a new unsaved scene with the sample. Press Play to use it.</summary>
        [MenuItem("Demo Kit/Create Sample Scene")]
        public static void CreateSampleScene()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            NewScene();
            new GameObject("Demo Kit Sample").AddComponent<DemoKitSample>();
        }

        /// <summary>Creates a theme asset that the kit picks up by itself. Change its colours and sizes in the Inspector.</summary>
        [MenuItem("Demo Kit/Create Theme Asset")]
        public static void CreateThemeAsset()
        {
            const string folder = "Assets/Resources/DemoKit";
            const string path = folder + "/DemoTheme.asset";
            if (AssetDatabase.LoadAssetAtPath<DemoTheme>(path) == null)
            {
                if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
                if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/Resources", "DemoKit");
                AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<DemoTheme>(), path);
                AssetDatabase.SaveAssets();
            }
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<DemoTheme>(path);
        }

        /// <summary>An empty unsaved scene with a floor, a wall, and light.</summary>
        internal static Camera NewScene()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.62f, 0.60f, 0.58f);
            RenderSettings.skybox = null;

            var light = new GameObject("Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 0.75f;
            light.color = new Color(1f, 0.97f, 0.93f);
            light.transform.rotation = Quaternion.Euler(48f, -28f, 0f);

            Block("Floor", new Vector3(0f, -0.05f, 2f), new Vector3(14f, 0.1f, 14f), new Color(0.60f, 0.55f, 0.50f));
            Block("Wall", new Vector3(0f, 2f, 5f), new Vector3(14f, 4.2f, 0.1f), new Color(0.80f, 0.77f, 0.73f));

            var camera = new GameObject("Main Camera") { tag = "MainCamera" }.AddComponent<Camera>();
            camera.gameObject.AddComponent<AudioListener>();
            camera.transform.position = new Vector3(0f, 1.6f, 0f);
            camera.nearClipPlane = 0.05f;
            camera.fieldOfView = 62f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.72f, 0.69f, 0.66f);
            return camera;
        }

        private static void Block(string name, Vector3 position, Vector3 size, Color color)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.position = position;
            go.transform.localScale = size;
            var renderer = go.GetComponent<Renderer>();
            // A copy of the default material of the active pipeline.
            var material = new Material(renderer.sharedMaterial) { name = name };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            renderer.sharedMaterial = material;
        }
    }
}
