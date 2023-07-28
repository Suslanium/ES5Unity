using System;
using System.IO;
using Engine;
using NIF;
using NIF.Converter;
using UnityEngine;

namespace Tests
{
    public class ModelLoadTest: MonoBehaviour
    {
        [SerializeField] private string dataFolderPath;
        [SerializeField] private string meshPath;

        private void Start()
        {
            var resourceManager = new ResourceManager(dataFolderPath);
            var textureManager = new TextureManager(resourceManager);
            var materialManager = new MaterialManager(textureManager);
            var envMapManager = new EnvironmentalMapManager(textureManager);
            var nif = new BinaryReader(resourceManager.GetFileOrNull(meshPath));
            var niFile = NiFile.ReadNif(meshPath, nif, 0);
            var niObjectBuilder = new NifObjectBuilder(niFile, materialManager, envMapManager);
            niObjectBuilder.BuildObject();
            nif.Close();
            //materialManager.ClearCachedMaterialsAndTextures();
            resourceManager.Close();
        }
    }
}