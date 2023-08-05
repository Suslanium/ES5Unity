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
        private readonly Dictionary<string, Texture2D> _metallicMapStore = new();
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
        
        public void PreloadMetallicMap(string metallicMapPath)
        {
            if (!_metallicMapStore.ContainsKey(metallicMapPath) && !_metallicMapTasks.TryGetValue(metallicMapPath, out var newTask))
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
        public Texture2D GetNormalMap(string texturePath)
        {
            if (_normalMapStore.TryGetValue(texturePath, out var normalMap))
            {
                return normalMap;
            }

            if (!_normalMapTasks.TryGetValue(texturePath, out var newTask))
            {
                newTask = StartLoadTextureTask(texturePath);
                _normalMapTasks.Add(texturePath, newTask);
            }

            var result = newTask.Result;
            var texture = result != null ? result.ToLinearTexture2D() : new Texture2D(1, 1);
            _normalMapStore.Add(texturePath, texture);
            _normalMapTasks.Remove(texturePath);
            return texture;
        }
        
        /// <summary>
        /// WARNING: This method should only be called from the main thread
        /// </summary>
        public Texture2D GetMetallicMap(string texturePath)
        {
            if (_metallicMapStore.TryGetValue(texturePath, out var metallicMap))
            {
                return metallicMap;
            }

            if (!_metallicMapTasks.TryGetValue(texturePath, out var newTask))
            {
                newTask = StartLoadTextureTask(texturePath);
                _metallicMapTasks.Add(texturePath, newTask);
            }

            var result = newTask.Result;
            var texture = result != null ? result.ToTexture2D() : new Texture2D(1, 1);
            _metallicMapStore.Add(texturePath, texture);
            _metallicMapTasks.Remove(texturePath);
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

            foreach (var texture in _metallicMapStore.Values)
            {
                Object.Destroy(texture);
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
            _metallicMapStore.Clear();
            _glowMapStore.Clear();
            _environmentalMapStore.Clear();
            _diffuseMapTasks.Clear();
            _normalMapTasks.Clear();
            _metallicMapTasks.Clear();
            _glowMapTasks.Clear();
            _environmentalMapTasks.Clear();
        }
    }
}