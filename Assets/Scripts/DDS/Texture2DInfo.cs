using System;
using System.Collections;
using System.IO;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DDS
{
    /// <summary>
    /// Stores information about a 2D texture.
    /// (Taken from https://github.com/ColeDeanShepherd/TESUnity/blob/f4d5e19f68da380da9da745356c7904f3428b9d6/Assets/Scripts/Core/Texture2DInfo.cs)
    /// </summary>
    public class Texture2DInfo
    {
        public readonly int Width;
        public readonly int Height;
        public readonly TextureFormat Format;
        public readonly bool HasMipmaps;
        public readonly byte[] RawData;

        public Texture2DInfo(int width, int height, TextureFormat format, bool hasMipmaps, byte[] rawData)
        {
            this.Width = width;
            this.Height = height;
            this.Format = format;
            this.HasMipmaps = hasMipmaps;
            this.RawData = rawData;
        }

        /// <summary>
        /// Creates a Unity Texture2D from this Texture2DInfo.
        /// </summary>
        public IEnumerator ToTexture2D(Action<Texture2D> onReadyCallback, bool linear = false)
        {
            var texture = new Texture2D(Width, Height, Format, HasMipmaps, linear);
            yield return null;

            if (RawData == null)
            {
                onReadyCallback(texture);
                yield break;
            }
            
            texture.LoadRawTextureData(RawData);
            yield return null;
            texture.Apply();
            yield return null;

            onReadyCallback(texture);
        }

        public IEnumerator ToLinearTexture2D(Action<Texture2D> onReadyCallback)
        {
            return ToTexture2D(onReadyCallback, true);
        }

        public IEnumerator ToCubemap(Action<Cubemap> onReadyCallback)
        {
            if (Width != Height)
                throw new InvalidDataException(
                    "Cubemap cannot be created from texture with non-equal width and height");
            
            var cubemap = new Cubemap(Width, Format, HasMipmaps);
            yield return null;

            Texture2D texture = null;
            var textureCoroutine = ToTexture2D(texture2D => { texture = texture2D; });
            while (textureCoroutine.MoveNext())
            {
                yield return null;
            }

            for (var i = 0; i < texture.mipmapCount; i++)
            {
                cubemap.SetPixels(texture.GetPixels(i), CubemapFace.NegativeX, i);
                yield return null;
                cubemap.SetPixels(texture.GetPixels(i), CubemapFace.NegativeY, i);
                yield return null;
                cubemap.SetPixels(texture.GetPixels(i), CubemapFace.NegativeZ, i);
                yield return null;
                cubemap.SetPixels(texture.GetPixels(i), CubemapFace.PositiveX, i);
                yield return null;
                cubemap.SetPixels(texture.GetPixels(i), CubemapFace.PositiveY, i);
                yield return null;
                cubemap.SetPixels(texture.GetPixels(i), CubemapFace.PositiveZ, i);
                yield return null;
            }

            cubemap.Apply();
            yield return null;
            Object.Destroy(texture);
            yield return null;
            onReadyCallback(cubemap);
        }
    }
}