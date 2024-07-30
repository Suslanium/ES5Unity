using System;
using System.IO;
using Pfim;
using UnityEngine;

namespace Textures
{
    public static class TextureReader
    {
        private static void SwapRedAndBlueChannelsRGBA32(byte[] data)
        {
            for (var i = 0; i < data.Length; i += 4)
            {
                (data[i], data[i + 2]) = (data[i + 2], data[i]);
            }
        }

        private static void SwapRedAndBlueChannelsRGB24(byte[] data)
        {
            for (var i = 0; i < data.Length; i += 3)
            {
                (data[i], data[i + 2]) = (data[i + 2], data[i]);
            }
        }
        
        /// <summary>
        /// Loads a DDS/TGA texture from a file.
        /// </summary>
        public static Texture2DInfo LoadTexture(string filePath)
        {
            return LoadTexture(File.Open(filePath, FileMode.Open, FileAccess.Read));
        }

        /// <summary>
        /// Loads a DDS/TGA texture from an input stream.
        /// </summary>
        public static Texture2DInfo LoadTexture(Stream inputStream)
        {
            using var texture = Pfimage.FromStream(inputStream, new PfimConfig(applyColorMap: false));
            if (texture.Compressed) texture.Decompress();
            TextureFormat format;

            switch (texture.Format)
            {
                case ImageFormat.Rgb8:
                    format = TextureFormat.R8;
                    break;
                case ImageFormat.Rgb24:
                    SwapRedAndBlueChannelsRGB24(texture.Data);
                    format = TextureFormat.RGB24;
                    break;
                case ImageFormat.Rgba32:
                    SwapRedAndBlueChannelsRGBA32(texture.Data);
                    format = TextureFormat.RGBA32;
                    break;
                case ImageFormat.Rgba16:
                case ImageFormat.R5g5b5:
                case ImageFormat.R5g6b5:
                case ImageFormat.R5g5b5a1:
                default:
                    throw new NotImplementedException($"Unsupported texture format: {texture.Format}");
            }

            return new Texture2DInfo(
                texture.Width,
                texture.Height,
                format,
                texture.MipMaps.Length > 1,
                texture.Data
            );
        }
    }
}