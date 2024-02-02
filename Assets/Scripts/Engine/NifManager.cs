using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using NIF;
using NIF.Converter;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Engine
{
    public class NifManager
    {
        private readonly Dictionary<string, GameObject> _nifPrefabs = new();
        private readonly Dictionary<string, Task<NiFile>> _niFileTasks = new();
        private readonly MaterialManager _materialManager;
        private readonly ResourceManager _resourceManager;
        private GameObject _prefabContainerObject;

        public NifManager(MaterialManager materialManager, ResourceManager resourceManager)
        {
            _materialManager = materialManager;
            _resourceManager = resourceManager;
        }

        private Task<NiFile> StartNiFileLoadingTask(string filePath)
        {
            return Task.Run(() =>
            {
                var fileStream = _resourceManager.GetFileOrNull(filePath);
                if (fileStream == null) return null;
                var fileReader = new BinaryReader(fileStream);
                var niFile = NiFile.ReadNif(filePath, fileReader, 0);
                fileReader.Close();
                fileStream.Close();
                return niFile;
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

        public IEnumerator InstantiateNif(string filePath, Action<GameObject> onReadyCallback)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                onReadyCallback(null);
                yield break;
            }

            FormatMeshString(ref filePath);
            EnsurePrefabContainerObjectExists();

            if (!_nifPrefabs.TryGetValue(filePath, out var prefab))
            {
                var prefabCoroutine = LoadNifPrefab(filePath, loadedPrefab => { prefab = loadedPrefab; });
                while (prefabCoroutine.MoveNext())
                {
                    yield return null;
                }

                _nifPrefabs[filePath] = prefab;
            }

            onReadyCallback(prefab != null ? Object.Instantiate(prefab) : null);
        }

        private IEnumerator LoadNifPrefab(string filePath, Action<GameObject> onReadyCallback)
        {
            PreloadNifFile(filePath);
            var task = _niFileTasks[filePath];
            
            while (!task.IsCompleted)
            {
                yield return null;
            }

            var file = task.Result;
            _niFileTasks.Remove(filePath);
            var objectBuilder = new NifObjectBuilder(file, _materialManager);
            yield return null;

            var prefabCoroutine = objectBuilder.BuildObject(prefab =>
            {
                if (prefab != null)
                {
                    prefab.transform.parent = _prefabContainerObject.transform;
                }

                onReadyCallback(prefab);
            });
            while (prefabCoroutine.MoveNext())
            {
                yield return null;
            }
        }

        /// <summary>
        /// WARNING: Call this only when models are not needed anymore(this will destroy every prefab)
        /// </summary>
        public void ClearModelCache()
        {
            foreach (var prefab in _nifPrefabs.Values)
            {
                Object.Destroy(prefab);
            }

            _nifPrefabs.Clear();
            _niFileTasks.Clear();
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