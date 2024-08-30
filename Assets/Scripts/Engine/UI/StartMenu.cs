using System;
using System.IO;
using System.Linq;
using Engine.MasterFile;
using Engine.Resource;
using TMPro;
using UnityEngine;

namespace Engine.UI
{
    public class StartMenu : MonoBehaviour
    {
        [SerializeField] private GameObject setupMenu;

        [SerializeField] private TMP_InputField pathText;

        [SerializeField] private TMP_InputField loadOrderText;

        [SerializeField] private GameObject initLoadingScreen;

        [SerializeField] private GameObject initErrorScreen;

        [SerializeField] private GameObject mainMenu;

        [SerializeField] private TMP_InputField cellText;

        [SerializeField] private UIManager uiManager;

        [SerializeField] private LoadingScreenManager loadingScreenManager;

        [SerializeField] private GameObject player;

        [SerializeField] private Camera mainCamera;

        private GameEngine _gameEngine;
        private ResourceManager _resourceManager;
        private MasterFileManager _masterFileManager;

        private void Start()
        {
            Settings.Initialize();
            if (Settings.DataPath != null && Settings.LoadOrder != null)
            {
                Initialize();
            }
            else
            {
                ShowSetupMenu();
            }
        }

        private void Initialize()
        {
            var path = Settings.DataPath;
            var masterFileNames = Settings.LoadOrder;
            try
            {
                _resourceManager = new ResourceManager(path);
                _masterFileManager = new MasterFileManager(masterFileNames
                    .Select(fileName => $"{path}{Path.DirectorySeparatorChar}{fileName}").ToList());
                _gameEngine = new GameEngine(_resourceManager, _masterFileManager, player, uiManager,
                    loadingScreenManager,
                    mainCamera);
                setupMenu.SetActive(false);
                initErrorScreen.SetActive(false);
                mainMenu.SetActive(false);
                initLoadingScreen.SetActive(true);
                _gameEngine.WaitForMasterFileInitialization(ShowMainMenu, ShowErrorScreen);
            }
            catch (Exception)
            {
                ShowErrorScreen();
            }
        }

        private void ShowErrorScreen()
        {
            setupMenu.SetActive(false);
            initLoadingScreen.SetActive(false);
            mainMenu.SetActive(false);
            initErrorScreen.SetActive(true);
        }

        public void ShowSetupMenu()
        {
            initLoadingScreen.SetActive(false);
            mainMenu.SetActive(false);
            initErrorScreen.SetActive(false);
            setupMenu.SetActive(true);
        }

        public void Setup()
        {
            var path = pathText.text;
            var loadOrder = loadOrderText.text.Split(',').ToList();
            Settings.SetDataPath(path);
            Settings.SetLoadOrder(loadOrder);
            Initialize();
        }

        private void ShowMainMenu()
        {
            setupMenu.SetActive(false);
            initLoadingScreen.SetActive(false);
            initErrorScreen.SetActive(false);
            mainMenu.SetActive(true);
        }

        private void HideMenus()
        {
            setupMenu.SetActive(false);
            initLoadingScreen.SetActive(false);
            initErrorScreen.SetActive(false);
            mainMenu.SetActive(false);
        }

        public void Load()
        {
            var cell = cellText.text;
            HideMenus();
            uiManager.FadeIn(() => { _gameEngine.LoadCell(cell, null, null); });
        }

        private void Update()
        {
            _gameEngine?.Update();
        }

        private void OnApplicationQuit()
        {
            _gameEngine?.OnStop();
            _resourceManager?.Close();
            _masterFileManager?.Close();
        }
    }
}