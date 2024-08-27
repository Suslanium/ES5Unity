using System.Collections.Generic;
using UnityEngine;

namespace Engine.Textures.TypeManager
{
    public interface ITextureTypeManager<out T> where T : Texture
    {
        void PreloadMap(string texturePath);

        IEnumerator<T> GetMap(string texturePath);
        
        void Clear();
    }
}