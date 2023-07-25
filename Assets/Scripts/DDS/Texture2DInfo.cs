using UnityEngine;

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
        public Texture2D ToTexture2D()
        {
            var texture = new Texture2D(Width, Height, Format, HasMipmaps);

            if (RawData == null) return texture;
            texture.LoadRawTextureData(RawData);
            texture.Apply();

            return texture;
        }
    }
}