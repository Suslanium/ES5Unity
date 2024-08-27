using System.Collections.Generic;
using Engine.Resource;
using UnityEngine;

namespace Engine.Textures.TypeManager
{
    public class DefaultTexture2DManager : TextureTypeManager<Texture2D>
    {
        private readonly bool _linearTextures;
        
        public DefaultTexture2DManager(ResourceManager resourceManager, bool linearTextures = false) : base(resourceManager)
        {
            _linearTextures = linearTextures;
        }

        public override IEnumerator<Texture2D> GetMap(string texturePath)
        {
            if (TextureStore.TryGetValue(texturePath, out var map))
            {
                yield return map;
                yield break;
            }

            if (!TaskStore.TryGetValue(texturePath, out var newTask))
            {
                newTask = StartLoadTextureTask(texturePath);
                TaskStore.TryAdd(texturePath, newTask);
                yield return null;
            }

            while (!newTask.IsCompleted)
            {
                yield return null;
            }

            var result = newTask.Result;
            yield return null;

            Texture2D texture;
            if (result != null)
            {
                var textureCoroutine = result.ToTexture2D(_linearTextures);
                while (textureCoroutine.MoveNext())
                {
                    yield return null;
                }
                
                texture = textureCoroutine.Current;
            }
            else
            {
                texture = new Texture2D(1, 1);
            }
            yield return null;

            TextureStore.TryAdd(texturePath, texture);
            yield return null;
            TaskStore.TryRemove(texturePath, out _);
            yield return null;
            yield return texture;
        }
    }
}