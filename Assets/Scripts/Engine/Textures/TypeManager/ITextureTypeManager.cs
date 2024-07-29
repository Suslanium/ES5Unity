using System;
using System.Collections;
using UnityEngine;

namespace Engine.Textures.TypeManager
{
    public interface ITextureTypeManager<out T> where T : Texture
    {
        void PreloadMap(string texturePath);

        IEnumerator GetMap(string texturePath, Action<T> onReadyCallback);
        
        void Clear();
    }
}