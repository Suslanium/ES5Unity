using System;
using System.Collections;
using Engine.Resource;
using UnityEngine;

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
                TaskStore.Add(texturePath, newTask);
            }

            while (!newTask.IsCompleted)
            {
                yield return null;
            }

            var result = newTask.Result;

            Cubemap texture = null;
            if (result != null)
            {
                var textureCoroutine = result.ToCubeMap(texture2D => { texture = texture2D; });
                while (textureCoroutine.MoveNext())
                {
                    yield return null;
                }
            }
            else
            {
                texture = new Cubemap(1, TextureFormat.RGBA32, false);
            }

            TextureStore.Add(texturePath, texture);
            TaskStore.Remove(texturePath);
            onReadyCallback(texture);
        }
    }
}