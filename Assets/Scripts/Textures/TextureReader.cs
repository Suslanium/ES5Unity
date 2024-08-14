using System;
using System.IO;
using Pfim;
using UnityEngine;

namespace Textures
{
    public enum TextureResolution
    {
        Full,
        Half,
        Quarter
    }

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

        private static byte[] RemoveFirstTwoMipMaps(byte[] data, int width, int height, TextureFormat format)
        {
            var firstMipMapSize = format switch
            {
                TextureFormat.R8 => width * height,
                TextureFormat.RGB24 => width * height * 3,
                TextureFormat.RGBA32 => width * height * 4,
                _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
            };
            var secondMipMapSize = firstMipMapSize / 4;
            var newLength = data.Length - firstMipMapSize - secondMipMapSize;
            var newData = new byte[newLength];
            Array.Copy(data, firstMipMapSize + secondMipMapSize, newData, 0, newLength);
            return newData;
        }

        private static byte[] RemoveFirstMipMap(byte[] data, int width, int height, TextureFormat format)
        {
            var mipMapSize = format switch
            {
                TextureFormat.R8 => width * height,
                TextureFormat.RGB24 => width * height * 3,
                TextureFormat.RGBA32 => width * height * 4,
                _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
            };
            var newLength = data.Length - mipMapSize;
            var newData = new byte[newLength];
            Array.Copy(data, mipMapSize, newData, 0, newLength);
            return newData;
        }

        /// <summary>
        /// Loads a DDS/TGA texture from a file.
        /// </summary>
        public static Texture2DInfo LoadTexture(string filePath, TextureResolution resolution = TextureResolution.Full)
        {
            return LoadTexture(File.Open(filePath, FileMode.Open, FileAccess.Read), resolution);
        }

        /// <summary>
        /// Loads a DDS/TGA texture from an input stream.
        /// </summary>
        public static Texture2DInfo LoadTexture(Stream inputStream,
            TextureResolution resolution = TextureResolution.Full)
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

            return resolution switch
            {
                TextureResolution.Quarter when texture.MipMaps.Length > 2 =>
                    new Texture2DInfo(
                        texture.Width / 4,
                        texture.Height / 4, format,
                        texture.MipMaps.Length > 3,
                        RemoveFirstTwoMipMaps(texture.Data, texture.Width, texture.Height, format)
                    ),
                TextureResolution.Quarter or TextureResolution.Half when texture.MipMaps.Length > 1 =>
                    new Texture2DInfo(
                        texture.Width / 2,
                        texture.Height / 2, format,
                        texture.MipMaps.Length > 2,
                        RemoveFirstMipMap(texture.Data, texture.Width, texture.Height, format)
                    ),
                _ => new Texture2DInfo(
                    texture.Width,
                    texture.Height,
                    format,
                    texture.MipMaps.Length > 1,
                    texture.Data)
            };
        }
    }
}