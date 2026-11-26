using System.IO;
using UnityEngine;

namespace SocialLeading.AllInOne
{
    /// <summary>Renders a camera into a picture file. Also works without a window.</summary>
    public static class ViewCapture
    {
        public static void Save(Camera camera, int width, int height, string path)
        {
            if (camera == null) return;
            var descriptor = new RenderTextureDescriptor(width, height, RenderTextureFormat.ARGB32, 24) { msaaSamples = 8, sRGB = true };
            RenderTexture target = RenderTexture.GetTemporary(descriptor);
            RenderTexture previousTarget = camera.targetTexture, previousActive = RenderTexture.active;
            float aspect = camera.aspect;
            camera.targetTexture = target;
            camera.aspect = width / (float)height;
            camera.Render();
            RenderTexture.active = target;
            var picture = new Texture2D(width, height, TextureFormat.RGB24, false);
            picture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
            picture.Apply();
            camera.targetTexture = previousTarget;
            camera.aspect = aspect;
            camera.ResetAspect();
            RenderTexture.active = previousActive;
            RenderTexture.ReleaseTemporary(target);
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            File.WriteAllBytes(path, picture.EncodeToPNG());
            Object.Destroy(picture);
        }
    }
}
