using System;
using System.Collections;
using Core;
using Engine.Door;
using Engine.MasterFile;
using Engine.Resource;
using Engine.Textures;
using UnityEngine;

namespace Engine
{
    public enum GameState
    {
        Loading,
        InGame,
        Paused
    }

    public class GameEngine
    {
        private const float DesiredWorkTimePerFrame = 1.0f / 200;
        private readonly ResourceManager _resourceManager;
        private readonly MasterFileManager _masterFileManager;
        private readonly TextureManager _textureManager;
        private readonly MaterialManager _materialManager;
        private readonly NifManager _nifManager;
        private readonly CellManager _cellManager;
        private readonly TemporalLoadBalancer _loadBalancer;
        private readonly UIManager _uiManager;
        private readonly GameObject _player;
        private readonly LoadingScreenManager _loadingScreenManager;
        public readonly Camera MainCamera;
        public Plane[] CameraPlanes { get; private set; }

        public GameState GameState
        {
            get => _backingState;
            set
            {
                switch (value)
                {
                    case GameState.Loading:
                        _uiManager.SetLoadingState();
                        _loadingScreenManager.ShowLoadingScreen();
                        break;
                    case GameState.InGame:
                        _loadingScreenManager.HideLoadingScreen();
                        if (!_player.activeSelf)
                            _player.SetActive(true);
                        _uiManager.SetInGameState();
                        break;
                    case GameState.Paused:
                        _uiManager.SetPausedState();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(value), value, null);
                }

                _backingState = value;
            }
        }

        private GameState _backingState;
        public DoorTeleport ActiveDoorTeleport;

        public GameEngine(ResourceManager resourceManager, MasterFileManager masterFileManager, GameObject player,
            UIManager uiManager, LoadingScreenManager loadingScreenManager, Camera mainCamera)
        {
            _resourceManager = resourceManager;
            _masterFileManager = masterFileManager;
            _textureManager = new TextureManager(_resourceManager);
            _materialManager = new MaterialManager(_textureManager);
            _nifManager = new NifManager(_materialManager, _resourceManager);
            _loadBalancer = new TemporalLoadBalancer();
            _cellManager = new CellManager(_masterFileManager, _nifManager, _loadBalancer, this, player);
            _loadingScreenManager = loadingScreenManager;
            _player = player;
            _uiManager = uiManager;
            uiManager.SetGameEngine(this);
            MainCamera = mainCamera;
            _loadingScreenManager.Initialize(masterFileManager, _nifManager, _loadBalancer);
        }

        // ReSharper disable Unity.PerformanceAnalysis
        public void LoadCell(string editorId, bool clearPrevious = false)
        {
            _player.SetActive(false);
            var startCellLoadingCoroutine = LoadCellCoroutine(editorId, clearPrevious);
            _loadBalancer.AddTask(startCellLoadingCoroutine);
        }

        // ReSharper disable Unity.PerformanceAnalysis
        public void LoadCell(uint formID, LoadCause loadCause, Vector3 startPosition, Quaternion startRotation,
            bool clearPrevious = false)
        {
            if (loadCause != LoadCause.OpenWorldLoad)
                _player.SetActive(false);
            var startCellLoadingCoroutine =
                LoadCellCoroutine(formID, loadCause, startPosition, startRotation, clearPrevious);
            _loadBalancer.AddTask(startCellLoadingCoroutine);
        }

        public void Update()
        {
            CameraPlanes = GeometryUtility.CalculateFrustumPlanes(MainCamera);
            _loadBalancer.RunTasks(DesiredWorkTimePerFrame);
        }

        private IEnumerator LoadCellCoroutine(string editorId, bool clearPrevious = false)
        {
            if (clearPrevious)
            {
                var clearCoroutine = DestroyAndClearEverything();
                while (clearCoroutine.MoveNext())
                {
                    yield return null;
                }
            }

            ActiveDoorTeleport = null;
            GameState = GameState.Loading;
            _cellManager.LoadCell(editorId);
        }

        private IEnumerator LoadCellCoroutine(uint formID, LoadCause loadCause, Vector3 startPosition,
            Quaternion startRotation, bool clearPrevious = false)
        {
            if (clearPrevious)
            {
                var clearCoroutine = DestroyAndClearEverything();
                while (clearCoroutine.MoveNext())
                {
                    yield return null;
                }
            }

            ActiveDoorTeleport = null;
            GameState = GameState.Loading;
            _cellManager.LoadCell(formID, loadCause, startPosition, startRotation);
        }

        private IEnumerator DestroyAndClearEverything()
        {
            var cellCoroutine = _cellManager.DestroyAllCells();
            while (cellCoroutine.MoveNext())
            {
                yield return null;
            }

            var nifCoroutine = _nifManager.ClearModelCache();
            while (nifCoroutine.MoveNext())
            {
                yield return null;
            }

            var materialCoroutine = _materialManager.ClearCachedMaterialsAndTextures();
            while (materialCoroutine.MoveNext())
            {
                yield return null;
            }
        }

        /// <summary>
        /// Call this only when exiting the game
        /// </summary>
        public void OnStop()
        {
            var clearCoroutine = DestroyAndClearEverything();
            //Iterating through the IEnumerator without using load balancer so that everything is going to happen instantly
            while (clearCoroutine.MoveNext())
            {
            }
        }
    }
}