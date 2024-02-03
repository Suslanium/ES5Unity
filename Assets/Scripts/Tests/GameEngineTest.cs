using System;
using System.IO;
using Engine;
using MasterFile;
using UnityEngine;

namespace Tests
{
    public class GameEngineTest: MonoBehaviour
    {
        [SerializeField] private string dataPath;
        [SerializeField] private string masterFilePath;
        [SerializeField] private string[] cellIds;
        [SerializeField] private uint cellFormId;
        private GameEngine _gameEngine;
        private ResourceManager _resourceManager;
        private BinaryReader _masterFileReader;
        private ESMasterFile _esMasterFile;

        private void Start()
        {
            _resourceManager = new ResourceManager(dataPath);
            _masterFileReader = new BinaryReader(File.Open(masterFilePath, FileMode.Open));
            _esMasterFile = new ESMasterFile(_masterFileReader);
            _gameEngine = new GameEngine(_resourceManager, _esMasterFile);
            foreach (var cellId in cellIds)
            {
                if (!string.IsNullOrEmpty(cellId)) _gameEngine.LoadInteriorCell(cellId);
            }
            if (cellFormId != 0) _gameEngine.LoadInteriorCell(cellFormId);
        }

        private void Update()
        {
            _gameEngine?.Update();
        }

        private void OnApplicationQuit()
        {
            _gameEngine.OnStop();
            _resourceManager.Close();
            _esMasterFile.Close();
        }
    }
}