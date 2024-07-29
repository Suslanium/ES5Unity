using System;
using System.Collections;
using UnityEngine;

namespace Engine.Textures.TypeManager
{
    public class DefaultTexture2DManager : TextureTypeManager<Texture2D>
    {
        public DefaultTexture2DManager(ResourceManager resourceManager) : base(resourceManager)
        {
        }

        public override IEnumerator GetMap(string texturePath, Action<Texture2D> onReadyCallback)
        {
            if (TextureStore.TryGetValue(texturePath, out var map))
            {
                onReadyCallback(map);
                yield break;
            }

            if (!TaskStore.TryGetValue(texturePath, out var newTask))
            {
                newTask = StartLoadTextureTask(texturePath);
                TaskStore.Add(texturePath, newTask);
            }

            while (!newTask.IsCompleted)
            {
                yield return null;
            }

            var result = newTask.Result;

            Texture2D texture = null;
            if (result != null)
            {
                var textureCoroutine = result.ToTexture2D(texture2D => { texture = texture2D; });
                while (textureCoroutine.MoveNext())
                {
                    yield return null;
                }
            }
            else
            {
                texture = new Texture2D(1, 1);
            }

            TextureStore.Add(texturePath, texture);
            TaskStore.Remove(texturePath);
            onReadyCallback(texture);
        }
    }
}