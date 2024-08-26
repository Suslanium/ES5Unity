using System.Collections.Generic;
using System.IO;
using System.Linq;
using Engine;
using Engine.MasterFile;
using Engine.Resource;
using TMPro;
using UnityEngine;

namespace Tests
{
    public class UIEngineTest : MonoBehaviour
    {
        [SerializeField] private GameObject loadUIPanel;

        [SerializeField] private GameObject player;

        [SerializeField] private Camera mainCamera;

        [SerializeField] private LoadingScreenManager loadingScreenManager;

        [SerializeField] private TMP_InputField pathText;

        [SerializeField] private TMP_InputField cellText;

        [SerializeField] private UIManager UIManager;

        [SerializeField] private List<string> masterFileNames;

        private GameEngine _gameEngine;
        private ResourceManager _resourceManager;
        private BinaryReader _masterFileReader;
        private MasterFileManager _masterFileManager;

        private void Update()
        {
            _gameEngine?.Update();
        }

        public void Load()
        {
#if !(DEVELOPMENT_BUILD || UNITY_EDITOR)
    Debug.unityLogger.logEnabled = false;
#endif
            var path = pathText.text;
            var cell = cellText.text;
            loadUIPanel.SetActive(false);
            _resourceManager = new ResourceManager(path);
            _masterFileManager = new MasterFileManager(masterFileNames
                .Select(fileName => $"{path}{Path.DirectorySeparatorChar}{fileName}").ToList());
            _gameEngine = new GameEngine(_resourceManager, _masterFileManager, player, UIManager, loadingScreenManager,
                mainCamera);
            UIManager.FadeIn(() => { _gameEngine.LoadCell(cell, null, null); });
        }

        private void OnApplicationQuit()
        {
            _gameEngine?.OnStop();
            _resourceManager?.Close();
            _masterFileManager?.Close();
        }
    }
}