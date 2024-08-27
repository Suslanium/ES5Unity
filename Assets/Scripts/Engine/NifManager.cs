using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Engine.Resource;
using Engine.Textures;
using NIF.Builder;
using NIF.Parser;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Engine
{
    public class NifManager
    {
        private readonly Dictionary<string, GameObject> _nifPrefabs = new();
        private readonly Dictionary<string, Task<NIF.Builder.Components.GameObject>> _niFileTasks = new();
        private readonly ResourceManager _resourceManager;
        private GameObject _prefabContainerObject;

        public NifManager(MaterialManager materialManager, TextureManager textureManager, ResourceManager resourceManager)
        {
            _resourceManager = resourceManager;
            NifObjectBuilder.Initialize(materialManager, textureManager);
        }

        private Task<NIF.Builder.Components.GameObject> StartNiFileLoadingTask(string filePath)
        {
            return Task.Run(() =>
            {
                var fileStream = _resourceManager.GetFileOrNull(filePath);
                if (fileStream == null) return null;
                var fileReader = new BinaryReader(fileStream);
                var niFile = NiFile.ReadNif(filePath, fileReader, 0);
                fileReader.Close();
                fileStream.Close();
                var objectBuilder = new NifObjectBuilder(niFile);
                var gameObject = objectBuilder.BuildObject();
                return gameObject;
            });
        }

        public void PreloadNifFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return;
            FormatMeshString(ref filePath);
            if (_nifPrefabs.ContainsKey(filePath)) return;

            if (_niFileTasks.TryGetValue(filePath, out var newTask)) return;
            newTask = StartNiFileLoadingTask(filePath);
            _niFileTasks.Add(filePath, newTask);
        }

        private void EnsurePrefabContainerObjectExists()
        {
            if (_prefabContainerObject != null) return;
            _prefabContainerObject = new GameObject("NIF Prefabs");
            _prefabContainerObject.SetActive(false);
        }

        public IEnumerator<GameObject> InstantiateNif(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                yield return null;
                yield break;
            }

            FormatMeshString(ref filePath);
            EnsurePrefabContainerObjectExists();

            if (!_nifPrefabs.TryGetValue(filePath, out var prefab))
            {
                var prefabCoroutine = LoadNifPrefab(filePath);
                while (prefabCoroutine.MoveNext())
                {
                    yield return null;
                }
                
                prefab = prefabCoroutine.Current;
                yield return null;
                _nifPrefabs[filePath] = prefab;
            }

            yield return prefab != null ? Object.Instantiate(prefab) : null;
        }

        private IEnumerator<GameObject> LoadNifPrefab(string filePath)
        {
            PreloadNifFile(filePath);
            var task = _niFileTasks[filePath];
            
            while (!task.IsCompleted)
            {
                yield return null;
            }

            var gameObject = task.Result;
            _niFileTasks.Remove(filePath);
            yield return null;

            if (gameObject == null)
            {
                yield return null;
                yield break;
            }

            var prefabCoroutine = gameObject.Create(_prefabContainerObject, true);
            while (prefabCoroutine.MoveNext())
            {
                yield return null;
            }
            var prefab = prefabCoroutine.Current;
            yield return prefab;
        }

        /// <summary>
        /// WARNING: Call this only when models are not needed anymore(this will destroy every prefab)
        /// </summary>
        public IEnumerator ClearModelCache()
        {
            foreach (var prefab in _nifPrefabs.Values)
            {
                Object.Destroy(prefab);
                yield return null;
            }

            _nifPrefabs.Clear();
            yield return null;
            _niFileTasks.Clear();
            yield return null;
        }

        private static void FormatMeshString(ref string path)
        {
            if (!path.StartsWith("meshes", true, CultureInfo.InvariantCulture))
            {
                path = $@"meshes{Path.DirectorySeparatorChar}{path}";
            }

            path = path.Replace("\0", string.Empty);
        }
    }
}