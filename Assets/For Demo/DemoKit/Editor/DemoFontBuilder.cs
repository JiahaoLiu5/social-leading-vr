using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace DemoKit.Editor
{
    /// <summary>
    /// Makes the static distance field fonts of the kit from the Inter files in DemoKit/Fonts. Only needed after changing
    /// the fonts or the character set. Run it in Unity 2022.3: newer versions read the result, but the reverse was not
    /// tested.
    /// </summary>
    public static class DemoFontBuilder
    {
        [MenuItem("Demo Kit/Rebuild Fonts")]
        public static void Build()
        {
            string root = KitRoot();
            string target = root + "/Runtime/Resources/DemoKit";
            Make(root + "/Fonts/Inter-Medium.otf", target + "/DemoKit Text SDF.asset");
            Make(root + "/Fonts/Inter-SemiBold.otf", target + "/DemoKit SemiBold SDF.asset");
            AssetDatabase.SaveAssets();
        }

        private static void Make(string fontPath, string assetPath)
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
            if (font == null) { Debug.LogError("DemoKit: font not found at " + fontPath); return; }
            TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(font, 74, 8, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, false);
            asset.TryAddCharacters(Characters(), out string missing);
            if (!string.IsNullOrEmpty(missing)) Debug.LogWarning("DemoKit: characters not in " + fontPath + ": " + missing);

            string name = Path.GetFileNameWithoutExtension(assetPath);
            asset.name = name;
            asset.atlasPopulationMode = AtlasPopulationMode.Static;
            asset.atlasTexture.name = name + " Atlas";
            asset.material.name = name + " Material";
            AssetDatabase.DeleteAsset(assetPath);
            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.AddObjectToAsset(asset.atlasTexture, asset);
            AssetDatabase.AddObjectToAsset(asset.material, asset);

            // A static font must not drag the source file into builds.
            var serialized = new SerializedObject(asset);
            foreach (string property in new[] { "m_SourceFontFile", "m_SourceFontFile_EditorRef" })
            {
                SerializedProperty p = serialized.FindProperty(property);
                if (p != null) p.objectReferenceValue = null;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(assetPath);
            Debug.Log("DemoKit: " + assetPath + " has " + asset.characterTable.Count + " characters");
        }

        private static string Characters()
        {
            var b = new StringBuilder();
            for (int c = 0x20; c <= 0x7E; c++) b.Append((char)c);
            for (int c = 0xA0; c <= 0xFF; c++) b.Append((char)c);
            b.Append("\u2013\u2014\u2018\u2019\u201C\u201D\u2022\u2026\u2190\u2191\u2192\u2193\u2212\u2264\u2265\u2248\u2713\u20AC\u2122");
            return b.ToString();
        }

        /// <summary>The folder that holds the kit, wherever it was put.</summary>
        internal static string KitRoot()
        {
            foreach (string guid in AssetDatabase.FindAssets("DemoKit t:AssemblyDefinitionAsset"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileName(path) == "DemoKit.asmdef") return Path.GetDirectoryName(Path.GetDirectoryName(path)).Replace('\\', '/');
            }
            return "Assets/DemoKit";
        }
    }
}
