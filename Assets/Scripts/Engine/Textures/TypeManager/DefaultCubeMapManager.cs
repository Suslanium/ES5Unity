using System;
using System.Collections;
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

        public override IEnumerator GetMap(string texturePath, Action<Cubemap> onReadyCallback)
        {
            if (TextureStore.TryGetValue(texturePath, out var envMap))
            {
                onReadyCallback(envMap);
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

            Cubemap texture = null;
            if (result != null)
            {
                var textureCoroutine = Coroutine.Get(result.ToCubeMap(cubeMap => { texture = cubeMap; }),
                    nameof(result.ToCubeMap));
                while (textureCoroutine.MoveNext())
                {
                    yield return null;
                }
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
            onReadyCallback(texture);
        }
    }
}