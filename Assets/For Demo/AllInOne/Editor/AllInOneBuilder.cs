using System;
using System.Collections.Generic;
using SocialLeading.AllInOne;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds everything of the integrated demo: content assets, prefabs, and the scene. Run it again after a change here;
/// the scene is never edited by hand.
/// </summary>
public static partial class AllInOneBuilder
{
    public const string Root = "Assets/For Demo/AllInOne/";
    public const string ScenePath = "Assets/For Demo/Scenes/AllInOne.unity";
    public const string PlayerName = "Demo AllInOne Player";

    private const string Content = Root + "Content/";
    private const string Prefabs = Root + "Prefabs/";
    private const string Materials = Content + "Materials/";
    private const string PlayerPath = Root + "Resources/" + PlayerName + ".prefab";
    private const string BodyPath = Prefabs + "Avatar Body.prefab";
    private const string BotPath = Prefabs + "Simulated Partner.prefab";
    private const string TaskPanelsPath = Prefabs + "Dyad Task Panels.prefab";
    private const string CharacterFolder = Root + "Resources/AllInOne/Characters/";
    private const string DemoRoot = "Assets/For Demo/";
    private const string SharedRoot = "Assets/Shared/";
    private const int AvatarCount = 5;

    [MenuItem("Social Leading/All-in-One/Rebuild Scene")]
    public static void Build()
    {
        Directories();
        AppConfig config = BuildContent();
        BuildCharacters();
        BuildPlayer(config);
        BuildBody();
        BuildBot();
        BuildTaskPanels();
        BuildScene(config);
        RegisterScene();
        AssetDatabase.SaveAssets();
        Debug.Log("All-in-One built: " + ScenePath);
    }

    private static void Directories()
    {
        foreach (string folder in new[] { Content, Content + "Thumbnails/", Materials, Prefabs, CharacterFolder })
            System.IO.Directory.CreateDirectory(folder);
        AssetDatabase.Refresh();
    }

    /// <summary>The integrated scene comes first and is the only enabled one. All others stay listed.</summary>
    private static void RegisterScene()
    {
        var scenes = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(ScenePath, true) };
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            if (scene.path != ScenePath) scenes.Add(new EditorBuildSettingsScene(scene.path, false));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    // Helpers -------------------------------------------------------------------------------------------------

    private static T Load<T>(string path) where T : UnityEngine.Object
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null) throw new InvalidOperationException("All-in-One: missing asset " + path);
        return asset;
    }

    /// <summary>Loads an asset or creates it, so that its id survives a rebuild.</summary>
    private static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null) return asset;
        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    /// <summary>Writes serialized fields by name, private ones included.</summary>
    private static void Set(UnityEngine.Object target, params (string field, object value)[] values)
    {
        var serialized = new SerializedObject(target);
        foreach ((string field, object value) in values)
        {
            SerializedProperty property = serialized.FindProperty(field);
            if (property == null) throw new InvalidOperationException(target.GetType().Name + " has no field " + field);
            Write(property, value);
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Write(SerializedProperty property, object value)
    {
        switch (value)
        {
            case null: property.objectReferenceValue = null; break;
            case UnityEngine.Object reference: property.objectReferenceValue = reference; break;
            case bool flag: property.boolValue = flag; break;
            case int number: property.intValue = number; break;
            case float number: property.floatValue = number; break;
            case string text: property.stringValue = text; break;
            case Vector3 vector: property.vector3Value = vector; break;
            case Array list:
                property.arraySize = list.Length;
                for (int i = 0; i < list.Length; i++) Write(property.GetArrayElementAtIndex(i), list.GetValue(i));
                break;
            default: throw new InvalidOperationException("Unsupported value for " + property.propertyPath);
        }
    }

    private static Transform Child(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child == null) throw new InvalidOperationException(parent.name + " has no child " + name);
        return child;
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("All-in-One: " + message);
    }
}
