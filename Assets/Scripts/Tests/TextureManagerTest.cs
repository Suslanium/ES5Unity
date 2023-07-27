using System;
using Engine;
using UnityEngine;
using UnityEngine.UI;

namespace Tests
{
    public class TextureManagerTest: MonoBehaviour
    {
        [SerializeField] private string dataFolderPath;
        [SerializeField] private string normalWithSpecularMapPath;
        [SerializeField] private RawImage normalMapImage;
        [SerializeField] private RawImage specularMapImage;

        private void Start()
        {
            var resourceManager = new ResourceManager(dataFolderPath);
            var textureManager = new TextureManager(resourceManager);
            var textures = textureManager.GetNormalAndSpecularMap(normalWithSpecularMapPath);
            normalMapImage.texture = textures.Item1;
            specularMapImage.texture = textures.Item2;
            resourceManager.Close();
        }
    }
}