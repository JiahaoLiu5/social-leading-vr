using System;
using System.IO;
using DemoKit.Sample;
using UnityEditor;
using UnityEngine;

namespace DemoKit.Editor
{
    /// <summary>
    /// Renders the sample panels to PNG files without entering play mode. In batch mode:
    /// Unity -batchmode -projectPath P -executeMethod DemoKit.Editor.DemoKitShots.Batch -demoKitShots OUTPUT_FOLDER
    /// (leave out -nographics, and do not add -quit: the method quits on its own).
    /// </summary>
    public static class DemoKitShots
    {
        public static void Batch()
        {
            int code = 0;
            try { Render(Argument("-demoKitShots", "DemoKitShots"), Argument("-demoKitPrefix", "kit_")); }
            catch (Exception e) { Debug.LogException(e); code = 1; }
            EditorApplication.Exit(code);
        }

        [MenuItem("Demo Kit/Render Sample Screenshots")]
        private static void FromMenu()
        {
            string folder = EditorUtility.SaveFolderPanel("Folder for the screenshots", "", "");
            if (!string.IsNullOrEmpty(folder)) Render(folder, "kit_");
        }

        public static void Render(string folder, string prefix)
        {
            Directory.CreateDirectory(folder);
            Camera camera = DemoKitMenu.NewScene();
            var sample = new GameObject("Demo Kit Sample").AddComponent<DemoKitSample>();
            sample.Build();
            var eyes = new Vector3(0f, 1.5f, 0f);
            sample.Arrange(eyes);
            foreach (DemoPanel p in new[] { sample.StartPanel, sample.Gallery, sample.Side }) p.Layout();
            camera.transform.position = eyes;

            var pages = DemoUi.Get<DemoPages>("pages");
            for (int i = 0; i < pages.Count; i++)
            {
                pages.Go(i);
                sample.StartPanel.Layout();
                Shot(camera, sample.StartPanel, Path.Combine(folder, prefix + "edit_" + (i + 1) + "_" + pages.Current + ".png"));
            }
            Shot(camera, sample.Gallery, Path.Combine(folder, prefix + "edit_gallery.png"));
            Shot(camera, sample.Side, Path.Combine(folder, prefix + "edit_side.png"));
        }

        /// <summary>Renders one panel from straight in front.</summary>
        public static void Shot(Camera camera, DemoPanel panel, string path, int width = 1600, int height = 1200)
        {
            Vector2 size = panel.SizeMetres;
            camera.fieldOfView = 50f;
            float need = Mathf.Max(size.y, size.x * height / width) * 1.16f;
            float distance = need * 0.5f / Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            camera.transform.SetPositionAndRotation(panel.transform.position - panel.transform.forward * distance, panel.transform.rotation);
            Shot(camera, path, width, height);
        }

        /// <summary>Renders a camera to a PNG file.</summary>
        public static void Shot(Camera camera, string path, int width, int height)
        {
            Canvas.ForceUpdateCanvases();
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 4 };
            RenderTexture before = camera.targetTexture;
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG());
            RenderTexture.active = null;
            camera.targetTexture = before;
            UnityEngine.Object.DestroyImmediate(image);
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
        }

        private static string Argument(string name, string fallback)
        {
            string[] args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, name);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : fallback;
        }
    }
}
