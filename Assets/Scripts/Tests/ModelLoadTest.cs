using Engine;
using UnityEngine;

namespace Tests
{
    public class ModelLoadTest: MonoBehaviour
    {
        [SerializeField] private string dataFolderPath;
        [SerializeField] private string[] meshPaths;

        private void Start()
        {
            var resourceManager = new ResourceManager(dataFolderPath);
            var textureManager = new TextureManager(resourceManager);
            var materialManager = new MaterialManager(textureManager);
            var nifManager = new NifManager(materialManager, resourceManager);
            foreach (var path in meshPaths)
            {
                //nifManager.InstantiateNif(path);
            }
            resourceManager.Close();
        }
    }
}