using System;
using System.Collections;
using Engine.Cell;
using Engine.Core;
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
        private readonly MaterialManager _materialManager;
        private readonly NifManager _nifManager;
        private readonly CellManager _cellManager;
        private readonly TemporalLoadBalancer _loadBalancer;
        private readonly UIManager _uiManager;
        private readonly PlayerManager _playerManager;
        private readonly LoadingScreenManager _loadingScreenManager;
        public readonly Camera MainCamera;
        public Plane[] CameraPlanes { get; private set; }

        public GameState GameState
        {
            get => _backingState;
            private set
            {
                switch (value)
                {
                    case GameState.Loading:
                        _uiManager.SetLoadingState();
                        if (_playerManager.PlayerActive)
                            _playerManager.PlayerActive = false;
                        _loadingScreenManager.ShowLoadingScreen();
                        break;
                    case GameState.InGame:
                        _loadingScreenManager.HideLoadingScreen();
                        if (!_playerManager.PlayerActive)
                            _playerManager.PlayerActive = true;
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
            var textureManager = new TextureManager(resourceManager);
            _materialManager = new MaterialManager(textureManager);
            _nifManager = new NifManager(_materialManager, resourceManager);
            _loadBalancer = new TemporalLoadBalancer();
            _playerManager = new PlayerManager(player);
            _cellManager = new CellManager(masterFileManager, _nifManager, textureManager,
                _loadBalancer, this,
                _playerManager);
            _loadingScreenManager = loadingScreenManager;
            _uiManager = uiManager;
            uiManager.SetGameEngine(this);
            MainCamera = mainCamera;
            _loadingScreenManager.Initialize(masterFileManager, _nifManager, _loadBalancer);
        }

        // ReSharper disable Unity.PerformanceAnalysis
        public void LoadCell(string editorId, LoadCause loadCause, Vector3 startPosition, Quaternion startRotation)
        {
            var loadCoroutine = LoadCellCoroutine(editorId, loadCause, startPosition, startRotation);
            _loadBalancer.AddTaskPriority(loadCoroutine);
        }

        private IEnumerator LoadCellCoroutine(string editorId, LoadCause loadCause, Vector3 startPosition,
            Quaternion startRotation)
        {
            if (loadCause != LoadCause.OpenWorldLoad)
            {
                var clearCoroutine = DestroyAndClearEverything();
                while (clearCoroutine.MoveNext())
                    yield return null;
            }

            ActiveDoorTeleport = null;
            if (loadCause != LoadCause.OpenWorldLoad)
                GameState = GameState.Loading;
            _cellManager.LoadCell(editorId, loadCause, startPosition, startRotation,
                () =>
                {
                    if (loadCause != LoadCause.OpenWorldLoad)
                        GameState = GameState.InGame;
                });
        }

        // ReSharper disable Unity.PerformanceAnalysis
        public void LoadCell(uint formID, LoadCause loadCause, Vector3 startPosition, Quaternion startRotation)
        {
            var loadCoroutine = LoadCellCoroutine(formID, loadCause, startPosition, startRotation);
            _loadBalancer.AddTaskPriority(loadCoroutine);
        }

        private IEnumerator LoadCellCoroutine(uint formID, LoadCause loadCause, Vector3 startPosition,
            Quaternion startRotation)
        {
            if (loadCause != LoadCause.OpenWorldLoad)
            {
                var clearCoroutine = DestroyAndClearEverything();
                while (clearCoroutine.MoveNext())
                    yield return null;
            }

            ActiveDoorTeleport = null;
            if (loadCause != LoadCause.OpenWorldLoad)
                GameState = GameState.Loading;
            _cellManager.LoadCell(formID, loadCause, startPosition, startRotation,
                () =>
                {
                    if (loadCause != LoadCause.OpenWorldLoad)
                        GameState = GameState.InGame;
                });
        }

        public void Update()
        {
            CameraPlanes = GeometryUtility.CalculateFrustumPlanes(MainCamera);
            _cellManager.Update();
            _loadBalancer.RunTasks(DesiredWorkTimePerFrame);
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