using Textures;
using UnityEngine;
using UnityEngine.UI;

namespace Tests
{
    public class TextureReaderTest : MonoBehaviour
    {
        [SerializeField] private string texturePath;
        [SerializeField] private RawImage rawImage;

        private void Start()
        {
            var textureInfo = TextureReader.LoadTexture(texturePath);
            var textureCoroutine = textureInfo.ToTexture2D();
            while (textureCoroutine.MoveNext())
            {}
            rawImage.texture = textureCoroutine.Current;
        }
    }
}