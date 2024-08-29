using System;
using System.Collections;
using Engine.Resource;
using UnityEngine;
using Coroutine = Engine.Core.Coroutine;

namespace Engine.Textures.TypeManager
{
    public class DefaultTexture2DManager : TextureTypeManager<Texture2D>
    {
        private readonly bool _linearTextures;

        public DefaultTexture2DManager(ResourceManager resourceManager, bool linearTextures = false) : base(
            resourceManager)
        {
            _linearTextures = linearTextures;
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
                TaskStore.TryAdd(texturePath, newTask);
                yield return null;
            }

            while (!newTask.IsCompleted)
            {
                yield return null;
            }

            var result = newTask.Result;
            yield return null;

            Texture2D texture = null;
            if (result != null)
            {
                var textureCoroutine = Coroutine.Get(
                    result.ToTexture2D(createdTexture => { texture = createdTexture; }, _linearTextures),
                    nameof(result.ToTexture2D));
                while (textureCoroutine.MoveNext())
                {
                    yield return null;
                }
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
            onReadyCallback(texture);
        }
    }
}