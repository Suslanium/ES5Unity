using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using DDS;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Engine.Textures.TypeManager
{
    public abstract class TextureTypeManager<T> : ITextureTypeManager<T> where T : Texture
    {
        protected readonly Dictionary<string, T> TextureStore = new();
        protected readonly Dictionary<string, Task<Texture2DInfo>> TaskStore = new();
        
        protected readonly ResourceManager ResourceManager;
        
        protected TextureTypeManager(ResourceManager resourceManager)
        {
            ResourceManager = resourceManager;
        }
        
        protected virtual Task<Texture2DInfo> StartLoadTextureTask(string texturePath)
        {
            return Task.Run(() =>
            {
                var fileStream = ResourceManager.GetFileOrNull(texturePath);
                var texture = fileStream != null
                    ? DDSReader.LoadDDSTexture(fileStream)
                    : null;
                fileStream?.Close();
                return texture;
            });
        }
        
        public virtual void PreloadMap(string texturePath)
        {
            if (TextureStore.ContainsKey(texturePath) || TaskStore.ContainsKey(texturePath))
                return;
            
            TaskStore[texturePath] = StartLoadTextureTask(texturePath);
        }

        public abstract IEnumerator GetMap(string texturePath, Action<T> onReadyCallback);
        
        public void Clear()
        {
            foreach (var texture in TextureStore.Values)
            {
                Object.Destroy(texture);
            }
            TextureStore.Clear();
            TaskStore.Clear();
        }
    }
}