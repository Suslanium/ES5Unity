using System.Collections.Generic;
using System.Threading.Tasks;
using DDS;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Engine
{
    public class TextureManager
    {
        private readonly Dictionary<string, Texture2D> _diffuseMapStore = new();
        private readonly Dictionary<string, Texture2D> _normalMapStore = new();
        private readonly Dictionary<string, Texture2D> _grayScaleSpecularMapStore = new();
        private readonly Dictionary<string, Dictionary<Color, Texture2D>> _tintedSpecularMapStore = new();
        private readonly Dictionary<string, Texture2D> _glowMapStore = new();
        private readonly Dictionary<string, Cubemap> _environmentalMapStore = new();
        private readonly Dictionary<string, Task<Texture2DInfo>> _diffuseMapTasks = new();
        private readonly Dictionary<string, Task<Texture2DInfo>> _normalMapTasks = new();
        private readonly Dictionary<string, Task<Texture2DInfo>> _metallicMapTasks = new();
        private readonly Dictionary<string, Task<Texture2DInfo>> _glowMapTasks = new();
        private readonly Dictionary<string, Task<Texture2DInfo>> _environmentalMapTasks = new();

        private readonly ResourceManager _resourceManager;

        public TextureManager(ResourceManager resourceManager)
        {
            _resourceManager = resourceManager;
        }

        public void PreloadDiffuseMap(string texturePath)
        {
            if (!_diffuseMapStore.ContainsKey(texturePath) && !_diffuseMapTasks.TryGetValue(texturePath, out var newTask))
            {
                newTask = StartLoadTextureTask(texturePath);
                _diffuseMapTasks.Add(texturePath, newTask);
            }
        }
        
        public void PreloadNormalMap(string texturePath)
        {
            if (!_normalMapStore.ContainsKey(texturePath) && !_normalMapTasks.TryGetValue(texturePath, out var newTask))
            {
                newTask = StartLoadTextureTask(texturePath);
                _normalMapTasks.Add(texturePath, newTask);
            }
        }
        
        public void PreloadMetallicMap(string normalMapPath, string metallicMapPath)
        {
            if (!_grayScaleSpecularMapStore.ContainsKey(normalMapPath) && !_metallicMapTasks.TryGetValue(metallicMapPath, out var newTask))
            {
                newTask = StartLoadTextureTask(metallicMapPath);
                _metallicMapTasks.Add(metallicMapPath, newTask);
            }
        }
        
        public void PreloadGlowMap(string texturePath)
        {
            if (!_glowMapStore.ContainsKey(texturePath) && !_glowMapTasks.TryGetValue(texturePath, out var newTask))
            {
                newTask = StartLoadTextureTask(texturePath);
                _glowMapTasks.Add(texturePath, newTask);
            }
        }

        private Task<Texture2DInfo> StartLoadTextureTask(string texturePath)
        {
            return Task.Run(() =>
            {
                var fileStream = _resourceManager.GetFileOrNull(texturePath);
                var texture = fileStream != null
                    ? DDSReader.LoadDDSTexture(fileStream)
                    : null;
                fileStream?.Close();
                return texture;
            });
        }

        /// <summary>
        /// WARNING: This method should only be called from the main thread
        /// </summary>
        public Texture2D GetDiffuseMap(string texturePath)
        {
            if (_diffuseMapStore.TryGetValue(texturePath, out var diffuseMap))
            {
                return diffuseMap;
            }

            if (!_diffuseMapTasks.TryGetValue(texturePath, out var newTask))
            {
                newTask = StartLoadTextureTask(texturePath);
                _diffuseMapTasks.Add(texturePath, newTask);
            }

            var result = newTask.Result;
            var texture = result != null ? result.ToTexture2D() : new Texture2D(1, 1);
            _diffuseMapStore.Add(texturePath, texture);
            _diffuseMapTasks.Remove(texturePath);
            return texture;
        }

        /// <summary>
        /// WARNING: This method should only be called from the main thread
        /// </summary>
        public Texture2D GetGlowMap(string texturePath)
        {
            if (_glowMapStore.TryGetValue(texturePath, out var glowMap))
            {
                return glowMap;
            }

            if (!_glowMapTasks.TryGetValue(texturePath, out var newTask))
            {
                newTask = StartLoadTextureTask(texturePath);
                _glowMapTasks.Add(texturePath, newTask);
            }

            var result = newTask.Result;
            var texture = result != null ? result.ToTexture2D() : new Texture2D(1, 1);
            _glowMapStore.Add(texturePath, texture);
            _glowMapTasks.Remove(texturePath);
            return texture;
        }

        /// <summary>
        /// WARNING: This method should only be called from the main thread
        /// </summary>
        public Cubemap GetEnvMap(string texturePath)
        {
            if (_environmentalMapStore.TryGetValue(texturePath, out var envMap))
            {
                return envMap;
            }

            if (!_environmentalMapTasks.TryGetValue(texturePath, out var newTask))
            {
                newTask = StartLoadTextureTask(texturePath);
                _environmentalMapTasks.Add(texturePath, newTask);
            }

            var result = newTask.Result;
            var texture = result != null ? result.ToCubemap() : new Cubemap(1, TextureFormat.RGBA32, false);
            _environmentalMapStore.Add(texturePath, texture);
            _environmentalMapTasks.Remove(texturePath);
            return texture;
        }

        /// <summary>
        /// Reads a *_n.dds file and returns normal map from RGB part of the file.
        /// Also caches alpha channel of normal map as a grayscale specular texture for later usage.
        /// If a valid metallic file is specified - specular map uses metallic rgb and normal map alpha.
        /// (Skyrim usually combines specular and normal map inside one *_n.dds texture)
        /// WARNING: This method should only be called from the main thread
        /// </summary>
        public Texture2D GetNormalMapAndExtractSpecular(string normalMapPath, string metallicMapPath)
        {
            if (_normalMapStore.TryGetValue(normalMapPath, out var cachedNormalMap))
            {
                return cachedNormalMap;
            }

            //Load normal map
            if (!_normalMapTasks.TryGetValue(normalMapPath, out var newTask))
            {
                newTask = StartLoadTextureTask(normalMapPath);
                _normalMapTasks.Add(normalMapPath, newTask);
            }

            var originalTexture = newTask.Result;
            if (originalTexture == null) return new Texture2D(1, 1);
            var originalMap = originalTexture.ToTexture2D();

            //Load metallic map(if specified)
            Texture2D metallicMap = null;
            if (!string.IsNullOrEmpty(metallicMapPath))
            {
                if (!_metallicMapTasks.TryGetValue(metallicMapPath, out var metallicMapTask))
                {
                    metallicMapTask = StartLoadTextureTask(metallicMapPath);
                    _metallicMapTasks.Add(metallicMapPath, metallicMapTask);
                }

                metallicMap = metallicMapTask.Result != null
                    ? metallicMapTask.Result.ToTexture2D()
                    : new Texture2D(1, 1);
            }

            //Initialize new textures
            var normalMap = new Texture2D(originalTexture.Width, originalTexture.Height, originalTexture.Format,
                originalTexture.HasMipmaps, true);
            var specularMap = new Texture2D(originalTexture.Width, originalTexture.Height, originalTexture.Format,
                originalTexture.HasMipmaps);

            //Resize metallic map to normal map size(if they don't match)
            if (metallicMap != null &&
                (metallicMap.width != originalMap.width || metallicMap.height != originalMap.height))
            {
                metallicMap = Resize(metallicMap, originalMap.width, originalMap.height);
            }

            //Start the copying process
            var originalPixels = originalMap.GetPixels(0);
            Color[] metallicPixels = null;
            if (metallicMap != null)
            {
                metallicPixels = metallicMap.GetPixels(0);
            }

            var normalPixels = new Color[originalPixels.Length];
            var specularPixels = new Color[originalPixels.Length];
            for (var i = 0; i < normalPixels.Length; i++)
            {
                //Copy rgb without alpha to a new normal map
                normalPixels[i] = new Color(originalPixels[i].r, originalPixels[i].g, originalPixels[i].b);
                if (metallicPixels == null)
                {
                    //If metallic map is not specified - use alpha from normal map
                    specularPixels[i] = new Color(originalPixels[i].a, originalPixels[i].a, originalPixels[i].a,
                        originalPixels[i].a);
                }
                else
                {
                    //If metallic map IS specified - use RGB from it and alpha from normal map
                    specularPixels[i] = new Color(metallicPixels[i].r, metallicPixels[i].g, metallicPixels[i].b,
                        originalPixels[i].a);
                }
            }

            normalMap.SetPixels(normalPixels, 0);
            specularMap.SetPixels(specularPixels, 0);

            normalMap.Apply();
            specularMap.Apply();

            //Cache normal map and grayscale specular and return ONLY normal map
            //(Specular will probably be used with tint, so there is no need to return it now)
            _normalMapStore.Add(normalMapPath, normalMap);
            _normalMapTasks.Remove(normalMapPath);
            if (!string.IsNullOrEmpty(metallicMapPath)) _metallicMapTasks.Remove(metallicMapPath);
            _grayScaleSpecularMapStore.Add(normalMapPath, specularMap);
            return normalMap;
        }

        /// <summary>
        /// Returns a specular map with specified tint. Specular map should be previously extracted from normal map via GetNormalMapAndExtractSpecular method, otherwise this method won't find the cached specular map and will return an empty texture.
        /// WARNING: This method should only be called from the main thread
        /// </summary>
        public Texture2D GetTintedSpecularMap(Color tint, string normalMapPath)
        {
            //Don't do additional processing if we don't need any tint
            if (tint == Color.white)
            {
                return _grayScaleSpecularMapStore.TryGetValue(normalMapPath, out var grayscaleSpecular)
                    ? grayscaleSpecular
                    : new Texture2D(1, 1);
            }

            //If we already have a this exact specular map with this exact tint - use it, otherwise take a base grayscale specular map and apply tint to it
            if (_tintedSpecularMapStore.TryGetValue(normalMapPath, out var tintedSpecularMaps))
            {
                return tintedSpecularMaps.TryGetValue(tint, out var tintedCachedTexture)
                    ? tintedCachedTexture
                    : GetTintedSpecularMapFromGrayscale(tint, normalMapPath);
            }
            else if (_grayScaleSpecularMapStore.ContainsKey(normalMapPath))
            {
                return GetTintedSpecularMapFromGrayscale(tint, normalMapPath);
            }
            else
            {
                return new Texture2D(1, 1);
            }
        }

        /// <summary>
        /// Applies tint to base grayscale specular map, saves the result to a new texture, caches it and returns it.
        /// </summary>
        private Texture2D GetTintedSpecularMapFromGrayscale(Color color, string normalMapPath)
        {
            var grayscaleSpecularMap = _grayScaleSpecularMapStore[normalMapPath];
            var tintedSpecularMap = new Texture2D(grayscaleSpecularMap.width, grayscaleSpecularMap.height,
                grayscaleSpecularMap.format, grayscaleSpecularMap.mipmapCount > 1);

            for (var mipMapLevel = 0; mipMapLevel < grayscaleSpecularMap.mipmapCount; mipMapLevel++)
            {
                var originalPixels = grayscaleSpecularMap.GetPixels(mipMapLevel);
                var specularPixels = new Color[originalPixels.Length];
                for (var i = 0; i < specularPixels.Length; i++)
                {
                    specularPixels[i] = originalPixels[i] * color;
                }

                tintedSpecularMap.SetPixels(specularPixels, mipMapLevel);
            }

            tintedSpecularMap.Apply();

            if (_tintedSpecularMapStore.TryGetValue(normalMapPath, out var tintedMap))
            {
                tintedMap.Add(color, tintedSpecularMap);
            }
            else
            {
                _tintedSpecularMapStore.Add(normalMapPath,
                    new Dictionary<Color, Texture2D> { { color, tintedSpecularMap } });
            }

            return tintedSpecularMap;
        }

        /// <summary>
        /// WARNING: Call this ONLY when textures are not needed anymore
        /// </summary>
        public void ClearCachedTextures()
        {
            foreach (var texture in _diffuseMapStore.Values)
            {
                Object.Destroy(texture);
            }

            foreach (var texture in _normalMapStore.Values)
            {
                Object.Destroy(texture);
            }

            foreach (var texture in _grayScaleSpecularMapStore.Values)
            {
                Object.Destroy(texture);
            }

            foreach (var tintedMapCollection in _tintedSpecularMapStore.Values)
            {
                foreach (var texture in tintedMapCollection.Values)
                {
                    Object.Destroy(texture);
                }
            }

            foreach (var texture in _glowMapStore.Values)
            {
                Object.Destroy(texture);
            }

            foreach (var texture in _environmentalMapStore.Values)
            {
                Object.Destroy(texture);
            }

            _diffuseMapStore.Clear();
            _normalMapStore.Clear();
            _grayScaleSpecularMapStore.Clear();
            _glowMapStore.Clear();
            _environmentalMapStore.Clear();
            _diffuseMapTasks.Clear();
            _normalMapTasks.Clear();
            _metallicMapTasks.Clear();
            _glowMapTasks.Clear();
            _environmentalMapTasks.Clear();
        }

        private static Texture2D Resize(Texture2D source, int newWidth, int newHeight)
        {
            source.filterMode = FilterMode.Point;
            RenderTexture rt = RenderTexture.GetTemporary(newWidth, newHeight);
            rt.filterMode = FilterMode.Point;
            RenderTexture.active = rt;
            Graphics.Blit(source, rt);
            Texture2D nTex = new Texture2D(newWidth, newHeight);
            nTex.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
            nTex.Apply();
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);
            Object.Destroy(source);
            return nTex;
        }
    }
}