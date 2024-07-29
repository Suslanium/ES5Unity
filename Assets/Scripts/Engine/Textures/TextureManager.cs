using System;
using System.Collections;
using System.Collections.Generic;
using Engine.Textures.TypeManager;
using UnityEngine;

namespace Engine.Textures
{
    public class TextureManager
    {
        private readonly Dictionary<TextureType, ITextureTypeManager<Texture>> _textureTypeHandlers;

        public TextureManager(ResourceManager resourceManager)
        {
            Texture.allowThreadedTextureCreation = true;
            //TODO replace this with DI or something
            _textureTypeHandlers = new Dictionary<TextureType, ITextureTypeManager<Texture>>
            {
                { TextureType.DIFFUSE, new DefaultTexture2DManager(resourceManager) },
                { TextureType.NORMAL, new DefaultTexture2DManager(resourceManager, true) },
                { TextureType.METALLIC, new DefaultTexture2DManager(resourceManager) },
                { TextureType.GLOW, new DefaultTexture2DManager(resourceManager) },
                { TextureType.ENVIRONMENTAL, new DefaultCubeMapManager(resourceManager) }
            };
        }

        public void PreloadMap(TextureType type, string texturePath)
        {
            var handler = _textureTypeHandlers[type];
            if (handler == null)
            {
                Debug.LogError($"Handler for texture type {type} not found");
                return;
            }

            handler.PreloadMap(texturePath);
        }

        /// <summary>
        /// WARNING: This method should only be called from the main thread
        /// </summary>
        public IEnumerator GetMap<T>(TextureType type, string texturePath, Action<T> onReadyCallback) where T : Texture
        {
            var handler = _textureTypeHandlers[type];
            if (handler == null)
            {
                Debug.LogError($"Handler for texture type {type} not found");
                return null;
            }

            if (handler is ITextureTypeManager<T> typedHandler)
                return typedHandler.GetMap(texturePath, onReadyCallback);

            Debug.LogError($"Handler for texture type {type} does not return textures of type {typeof(T)}");
            return null;
        }

        /// <summary>
        /// WARNING: Call this ONLY when textures are not needed anymore
        /// </summary>
        public IEnumerator ClearCachedTextures()
        {
            foreach (var textureTypeHandler in _textureTypeHandlers.Values)
            {
                textureTypeHandler.Clear();

                yield return null;
            }
        }
    }
}