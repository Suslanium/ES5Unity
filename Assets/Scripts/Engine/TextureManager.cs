using System;
using System.Collections.Generic;
using DDS;
using UnityEngine;

namespace Engine
{
    public class TextureManager
    {
        private readonly Dictionary<string, Texture2D> _diffuseMapStore = new();
        private readonly Dictionary<string, Texture2D> _normalMapStore = new();
        private readonly Dictionary<string, Texture2D> _specularMapStore = new();
        private readonly Dictionary<string, Texture2D> _glowMapStore = new();
        private readonly Dictionary<string, Texture2D> _environmentalMapStore = new();

        private readonly ResourceManager _resourceManager;

        public TextureManager(ResourceManager resourceManager)
        {
            _resourceManager = resourceManager;
        }

        public Texture2D GetDiffuseMap(string texturePath)
        {
            if (_diffuseMapStore.TryGetValue(texturePath, out var diffuseMap))
            {
                return diffuseMap;
            }

            var fileStream = _resourceManager.GetFileOrNull(texturePath);
            var texture = fileStream != null ? DDSReader.LoadDDSTexture(fileStream).ToTexture2D() : new Texture2D(1, 1);
            fileStream?.Close();
            _diffuseMapStore.Add(texturePath, texture);
            return texture;
        }

        public Texture2D GetGlowMap(string texturePath)
        {
            if (_glowMapStore.TryGetValue(texturePath, out var glowMap))
            {
                return glowMap;
            }

            var fileStream = _resourceManager.GetFileOrNull(texturePath);
            var texture = fileStream != null ? DDSReader.LoadDDSTexture(fileStream).ToTexture2D() : new Texture2D(1, 1);
            fileStream?.Close();
            _glowMapStore.Add(texturePath, texture);
            return texture;
        }

        public Texture2D GetEnvMap(string texturePath)
        {
            if (_environmentalMapStore.TryGetValue(texturePath, out var envMap))
            {
                return envMap;
            }

            var fileStream = _resourceManager.GetFileOrNull(texturePath);
            var texture = fileStream != null ? DDSReader.LoadDDSTexture(fileStream).ToTexture2D() : new Texture2D(1, 1);
            fileStream?.Close();
            _environmentalMapStore.Add(texturePath, texture);
            return texture;
        }

        /// <summary>
        /// Reads a *_n.dds file "as-is", without extracting anything from alpha channel (see GetNormalAndSpecularMap)
        /// </summary>
        public Texture2D GetNormalMapOnly(string texturePath)
        {
            if (_normalMapStore.TryGetValue(texturePath, out var normalMap))
            {
                return normalMap;
            }

            var fileStream = _resourceManager.GetFileOrNull(texturePath);
            var texture = fileStream != null ? DDSReader.LoadDDSTexture(fileStream).ToTexture2D() : new Texture2D(1, 1);
            fileStream?.Close();
            _normalMapStore.Add(texturePath, texture);
            return texture;
        }

        /// <summary>
        /// Reads a *_n.dds file and extracts normal map from RGB part of the file with specular map from alpha channel of the file. Use this only if the specular flag is set.
        /// </summary>
        public Tuple<Texture2D, Texture2D> GetNormalAndSpecularMap(string texturePath)
        {
            if (_specularMapStore.TryGetValue(texturePath, out var value))
            {
                return Tuple.Create(_normalMapStore[texturePath], value);
            }

            var fileStream = _resourceManager.GetFileOrNull(texturePath);
            if (fileStream == null) return Tuple.Create(new Texture2D(1, 1), new Texture2D(1, 1));

            var originalTexture = DDSReader.LoadDDSTexture(fileStream);
            var normalMap = new Texture2D(originalTexture.Width, originalTexture.Height, originalTexture.Format,
                originalTexture.HasMipmaps);
            var specularMap = new Texture2D(originalTexture.Width, originalTexture.Height, originalTexture.Format,
                originalTexture.HasMipmaps);
            var originalMap = originalTexture.ToTexture2D();

            for (var mipMapLevel = 0; mipMapLevel < originalMap.mipmapCount; mipMapLevel++)
            {
                var originalPixels = originalMap.GetPixels(mipMapLevel);
                var normalPixels = new Color[originalPixels.Length];
                var specularPixels = new Color[originalPixels.Length];
                for (var i = 0; i < normalPixels.Length; i++)
                {
                    normalPixels[i] = new Color(originalPixels[i].r, originalPixels[i].g, originalPixels[i].b);
                    specularPixels[i] = new Color(originalPixels[i].a, originalPixels[i].a, originalPixels[i].a);
                }

                normalMap.SetPixels(normalPixels, mipMapLevel);
                specularMap.SetPixels(specularPixels, mipMapLevel);
            }

            normalMap.Apply();
            specularMap.Apply();
            _normalMapStore.Add(texturePath, normalMap);
            _specularMapStore.Add(texturePath, specularMap);
            return Tuple.Create(normalMap, specularMap);
        }

        public void ClearCachedTextures()
        {
            _diffuseMapStore.Clear();
            _normalMapStore.Clear();
            _specularMapStore.Clear();
            _glowMapStore.Clear();
            _environmentalMapStore.Clear();
        }
    }
}