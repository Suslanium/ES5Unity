using DDS;
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
            //var textureInfo = DDSReader.LoadDDSTexture(texturePath);
            //var texture = textureInfo.ToTexture2D();
            //rawImage.texture = texture;
        }
    }
}