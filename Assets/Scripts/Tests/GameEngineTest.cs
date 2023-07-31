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
        [SerializeField] private string cellId;
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
            _gameEngine.LoadInteriorCell(cellId);
        }

        private void Update()
        {
            _gameEngine?.Update();
        }

        private void OnApplicationQuit()
        {
            _gameEngine.OnStop();
            _resourceManager.Close();
            _masterFileReader.Close();
        }
    }
}