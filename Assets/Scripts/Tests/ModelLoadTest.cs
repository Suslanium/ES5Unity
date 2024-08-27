using System.Diagnostics;
using Engine;
using Engine.Resource;
using Engine.Textures;
using UnityEngine;
using Logger = Engine.Core.Logger;

namespace Tests
{
    public class ModelLoadTest : MonoBehaviour
    {
        [SerializeField] private string dataFolderPath;
        [SerializeField] private string[] meshPaths;
        private NifManager _nifManager;
        private ResourceManager _resourceManager;
        private readonly Stopwatch _stopwatch = new();

        private void Start()
        {
            var resourceManager = new ResourceManager(dataFolderPath);
            var textureManager = new TextureManager(resourceManager);
            var materialManager = new MaterialManager(textureManager);
            var nifManager = new NifManager(materialManager, textureManager, resourceManager);
            _nifManager = nifManager;
            _resourceManager = resourceManager;
            Invoke(nameof(InstantiateMeshes), 1f);
        }

        private void InstantiateMeshes()
        {
            foreach (var path in meshPaths)
            {
                _stopwatch.Reset();
                _stopwatch.Start();
                var iterator = _nifManager.InstantiateNif(path);
                while (iterator.MoveNext())
                {
                }
                Logger.Log($"{path} loaded in {_stopwatch.ElapsedMilliseconds} ms");
                _stopwatch.Stop();
            }
            _resourceManager.Close();
        }
    }
}