using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Textures
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
            Width = width;
            Height = height;
            Format = format;
            HasMipmaps = hasMipmaps;
            RawData = rawData;
        }

        /// <summary>
        /// Creates a Unity Texture2D from this Texture2DInfo.
        /// </summary>
        public IEnumerator<Texture2D> ToTexture2D(bool linear = false)
        {
            var texture = new Texture2D(Width, Height, Format, HasMipmaps, linear);
            yield return null;

            if (RawData == null)
            {
                yield return texture;
                yield break;
            }
            
            texture.LoadRawTextureData(RawData);
            yield return null;
            texture.Apply();
            yield return null;

            yield return texture;
        }

        public IEnumerator<Cubemap> ToCubeMap()
        {
            if (Width != Height)
                throw new InvalidDataException(
                    "CubeMap cannot be created from texture with non-equal width and height");
            
            var cubeMap = new Cubemap(Width, Format, HasMipmaps);
            yield return null;

            var textureCoroutine = ToTexture2D();
            while (textureCoroutine.MoveNext())
            {
                yield return null;
            }
            
            var texture = textureCoroutine.Current;
            yield return null;
            
            if (texture == null)
            {
                yield return cubeMap;
                yield break;
            }

            for (var i = 0; i < texture.mipmapCount; i++)
            {
                cubeMap.SetPixels(texture.GetPixels(i), CubemapFace.NegativeX, i);
                yield return null;
                cubeMap.SetPixels(texture.GetPixels(i), CubemapFace.NegativeY, i);
                yield return null;
                cubeMap.SetPixels(texture.GetPixels(i), CubemapFace.NegativeZ, i);
                yield return null;
                cubeMap.SetPixels(texture.GetPixels(i), CubemapFace.PositiveX, i);
                yield return null;
                cubeMap.SetPixels(texture.GetPixels(i), CubemapFace.PositiveY, i);
                yield return null;
                cubeMap.SetPixels(texture.GetPixels(i), CubemapFace.PositiveZ, i);
                yield return null;
            }

            cubeMap.Apply();
            yield return null;
            Object.Destroy(texture);
            yield return null;
            yield return cubeMap;
        }
    }
}