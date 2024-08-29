using System.Collections.Generic;
using Engine.Resource;
using UnityEngine;
using Coroutine = Engine.Core.Coroutine;

namespace Engine.Textures.TypeManager
{
    public class DefaultCubeMapManager : TextureTypeManager<Cubemap>
    {
        public DefaultCubeMapManager(ResourceManager resourceManager) : base(resourceManager)
        {
        }

        public override IEnumerator<Cubemap> GetMap(string texturePath)
        {
            if (TextureStore.TryGetValue(texturePath, out var envMap))
            {
                yield return envMap;
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

            Cubemap texture;
            if (result != null)
            {
                var textureCoroutine = Coroutine.Get(result.ToCubeMap(), nameof(result.ToCubeMap));
                while (textureCoroutine.MoveNext())
                {
                    yield return null;
                }

                texture = textureCoroutine.Current;
            }
            else
            {
                texture = new Cubemap(1, TextureFormat.RGBA32, false);
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