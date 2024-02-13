using System;
using System.Collections.Generic;
using System.Collections;
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
            Texture.allowThreadedTextureCreation = true;
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
        public IEnumerator GetDiffuseMap(string texturePath, Action<Texture2D> onReadyCallback)
        {
            if (_diffuseMapStore.TryGetValue(texturePath, out var diffuseMap))
            {
                onReadyCallback(diffuseMap);
                yield break;
            }

            if (!_diffuseMapTasks.TryGetValue(texturePath, out var newTask))
            {
                newTask = StartLoadTextureTask(texturePath);
                _diffuseMapTasks.Add(texturePath, newTask);
            }

            while (!newTask.IsCompleted)
            {
                yield return null;
            }

            var result = newTask.Result;
            
            Texture2D texture = null;
            if (result != null)
            {
                var textureCoroutine = result.ToTexture2D(texture2D =>
                {
                    texture = texture2D;
                });
                while (textureCoroutine.MoveNext())
                {
                    yield return null;
                }
            }
            else
            {
                texture = new Texture2D(1, 1);
            }

            _diffuseMapStore.Add(texturePath, texture);
            _diffuseMapTasks.Remove(texturePath);
            onReadyCallback(texture);
        }
        
        /// <summary>
        /// WARNING: This method should only be called from the main thread
        /// </summary>
        public IEnumerator GetNormalMap(string texturePath, Action<Texture2D> onReadyCallback)
        {
            if (_normalMapStore.TryGetValue(texturePath, out var normalMap))
            {
                onReadyCallback(normalMap);
                yield break;
            }

            if (!_normalMapTasks.TryGetValue(texturePath, out var newTask))
            {
                newTask = StartLoadTextureTask(texturePath);
                _normalMapTasks.Add(texturePath, newTask);
            }

            while (!newTask.IsCompleted)
            {
                yield return null;
            }
            
            var result = newTask.Result;
            
            Texture2D texture = null;
            if (result != null)
            {
                var textureCoroutine = result.ToLinearTexture2D(texture2D =>
                {
                    texture = texture2D;
                });
                while (textureCoroutine.MoveNext())
                {
                    yield return null;
                }
            }
            else
            {
                texture = new Texture2D(1, 1);
            }
            
            _normalMapStore.Add(texturePath, texture);
            _normalMapTasks.Remove(texturePath);
            onReadyCallback(texture);
        }
        
        /// <summary>
        /// WARNING: This method should only be called from the main thread
        /// </summary>
        public IEnumerator GetMetallicMap(string texturePath, Action<Texture2D> onReadyCallback)
        {
            if (_metallicMapStore.TryGetValue(texturePath, out var metallicMap))
            {
                onReadyCallback(metallicMap);
                yield break;
            }

            if (!_metallicMapTasks.TryGetValue(texturePath, out var newTask))
            {
                newTask = StartLoadTextureTask(texturePath);
                _metallicMapTasks.Add(texturePath, newTask);
            }

            while (!newTask.IsCompleted)
            {
                yield return null;
            }
            
            var result = newTask.Result;
            
            Texture2D texture = null;
            if (result != null)
            {
                var textureCoroutine = result.ToTexture2D(texture2D =>
                {
                    texture = texture2D;
                });
                while (textureCoroutine.MoveNext())
                {
                    yield return null;
                }
            }
            else
            {
                texture = new Texture2D(1, 1);
            }
            
            _metallicMapStore.Add(texturePath, texture);
            _metallicMapTasks.Remove(texturePath);
            onReadyCallback(texture);
        }

        /// <summary>
        /// WARNING: This method should only be called from the main thread
        /// </summary>
        public IEnumerator GetGlowMap(string texturePath, Action<Texture2D> onReadyCallback)
        {
            if (_glowMapStore.TryGetValue(texturePath, out var glowMap))
            {
                onReadyCallback(glowMap);
                yield break;
            }

            if (!_glowMapTasks.TryGetValue(texturePath, out var newTask))
            {
                newTask = StartLoadTextureTask(texturePath);
                _glowMapTasks.Add(texturePath, newTask);
            }

            while (!newTask.IsCompleted)
            {
                yield return null;
            }
            
            var result = newTask.Result;
            
            Texture2D texture = null;
            if (result != null)
            {
                var textureCoroutine = result.ToTexture2D(texture2D =>
                {
                    texture = texture2D;
                });
                while (textureCoroutine.MoveNext())
                {
                    yield return null;
                }
            }
            else
            {
                texture = new Texture2D(1, 1);
            }
            
            _glowMapStore.Add(texturePath, texture);
            _glowMapTasks.Remove(texturePath);
            onReadyCallback(texture);
        }

        /// <summary>
        /// WARNING: This method should only be called from the main thread
        /// </summary>
        public IEnumerator GetEnvMap(string texturePath, Action<Cubemap> onReadyCallback)
        {
            if (_environmentalMapStore.TryGetValue(texturePath, out var envMap))
            {
                onReadyCallback(envMap);
                yield break;
            }

            if (!_environmentalMapTasks.TryGetValue(texturePath, out var newTask))
            {
                newTask = StartLoadTextureTask(texturePath);
                _environmentalMapTasks.Add(texturePath, newTask);
            }

            while (!newTask.IsCompleted)
            {
                yield return null;
            }
            
            var result = newTask.Result;
            
            Cubemap texture = null;
            if (result != null)
            {
                var textureCoroutine = result.ToCubemap(texture2D =>
                {
                    texture = texture2D;
                });
                while (textureCoroutine.MoveNext())
                {
                    yield return null;
                }
            }
            else
            {
                texture = new Cubemap(1, TextureFormat.RGBA32, false);
            }
            
            _environmentalMapStore.Add(texturePath, texture);
            _environmentalMapTasks.Remove(texturePath);
            onReadyCallback(texture);
        }

        /// <summary>
        /// WARNING: Call this ONLY when textures are not needed anymore
        /// </summary>
        public IEnumerator ClearCachedTextures()
        {
            foreach (var texture in _diffuseMapStore.Values)
            {
                Object.Destroy(texture);
            }
            yield return null;

            foreach (var texture in _normalMapStore.Values)
            {
                Object.Destroy(texture);
            }
            yield return null;

            foreach (var texture in _metallicMapStore.Values)
            {
                Object.Destroy(texture); 
            }
            yield return null;

            foreach (var texture in _glowMapStore.Values)
            {
                Object.Destroy(texture);
            }
            yield return null;

            foreach (var texture in _environmentalMapStore.Values)
            {
                Object.Destroy(texture);
            }
            yield return null;

            _diffuseMapStore.Clear();
            yield return null;
            _normalMapStore.Clear();
            yield return null;
            _metallicMapStore.Clear();
            yield return null;
            _glowMapStore.Clear();
            yield return null;
            _environmentalMapStore.Clear();
            yield return null;
            _diffuseMapTasks.Clear();
            yield return null;
            _normalMapTasks.Clear();
            yield return null;
            _metallicMapTasks.Clear();
            yield return null;
            _glowMapTasks.Clear();
            yield return null;
            _environmentalMapTasks.Clear();
            yield return null;
        }
    }
}