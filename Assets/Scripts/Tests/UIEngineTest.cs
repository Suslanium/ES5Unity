using System.IO;
using Engine;
using MasterFile;
using TMPro;
using UnityEngine;

namespace Tests
{
    public class UIEngineTest : MonoBehaviour
    {
        [SerializeField] private GameObject loadUIPanel;

        [SerializeField] private GameObject characterControls;

        [SerializeField] private GameObject player;

        [SerializeField] private TMP_InputField pathText;

        [SerializeField] private TMP_InputField cellText;

        private GameEngine _gameEngine;
        private ResourceManager _resourceManager;
        private BinaryReader _masterFileReader;
        private ESMasterFile _esMasterFile;

        private void Update()
        {
            _gameEngine?.Update();
        }

        public void Load()
        {
            var path = pathText.text;
            var cell = cellText.text;
            loadUIPanel.SetActive(false);
            characterControls.SetActive(true);
            player.SetActive(true);
            _resourceManager = new ResourceManager(path);
            _masterFileReader =
                new BinaryReader(File.Open($"{path}{Path.DirectorySeparatorChar}Skyrim.esm", FileMode.Open));
            _esMasterFile = new ESMasterFile(_masterFileReader);
            _gameEngine = new GameEngine(_resourceManager, _esMasterFile);
            _gameEngine.LoadInteriorCell(cell);
        }
        
        private void OnApplicationQuit()
        {
            _gameEngine?.OnStop();
            _resourceManager?.Close();
            _masterFileReader?.Close();
        }
    }
}