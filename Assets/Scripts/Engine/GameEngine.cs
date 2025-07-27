using System;
using System.Collections;
using Engine.Cell;
using Engine.Core;
using Engine.Door;
using Engine.MasterFile;
using Engine.Resource;
using Engine.Textures;
using Engine.UI;
using UnityEngine;
using Coroutine = Engine.Core.Coroutine;

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
        private readonly MaterialManager _materialManager;
        private readonly NifManager _nifManager;
        private readonly CellManager _cellManager;
        private readonly TemporalLoadBalancer _loadBalancer;
        private readonly UIManager _uiManager;
        private readonly PlayerManager _playerManager;
        private readonly LoadingScreenManager _loadingScreenManager;
        private readonly MasterFileManager _masterFileManager;
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
                        _loadBalancer.DesiredWorkTimePerFrame = Settings.LoadingDesiredWorkTimePerFrame;
                        _loadingScreenManager.ShowLoadingScreen();
                        break;
                    case GameState.InGame:
                        _loadingScreenManager.HideLoadingScreen();
                        if (!_playerManager.PlayerActive)
                            _playerManager.PlayerActive = true;
                        _loadBalancer.DesiredWorkTimePerFrame = Settings.InGameDesiredWorkTimePerFrame;
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
            _masterFileManager = masterFileManager;
            _nifManager = new NifManager(_materialManager, textureManager, resourceManager);
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

        public void WaitForMasterFileInitialization(Action onReadyCallback, Action onErrorCallback)
        {
            var loadCoroutine = WaitForMasterFileInitializationCoroutine(onReadyCallback, onErrorCallback);
            _loadBalancer.AddTaskPriority(loadCoroutine);
        }

        private IEnumerator WaitForMasterFileInitializationCoroutine(Action onReadyCallback, Action onErrorCallback)
        {
            var initializationTask = _masterFileManager.AwaitInitialization();
            while (!initializationTask.IsCompleted)
                yield return null;
            if (initializationTask.IsFaulted)
            {
                onErrorCallback();
            }
            else
            {
                onReadyCallback();
            }
        }

        public void LoadCell(string editorId, Vector3? startPosition, Quaternion? startRotation)
        {
            var loadCoroutine = LoadCellCoroutine(editorId, startPosition, startRotation);
            _loadBalancer.AddTaskPriority(loadCoroutine);
        }

        private IEnumerator LoadCellCoroutine(string editorId, Vector3? startPosition,
            Quaternion? startRotation)
        {
            var clearCoroutine = Coroutine.Get(DestroyAndClearEverything(), nameof(DestroyAndClearEverything));
            while (clearCoroutine.MoveNext())
                yield return null;

            ActiveDoorTeleport = null;
            GameState = GameState.Loading;
            _cellManager.LoadCell(editorId, startPosition, startRotation,
                () => { GameState = GameState.InGame; });
        }

        public void LoadCell(uint formID, Vector3? startPosition, Quaternion? startRotation)
        {
            var loadCoroutine = LoadCellCoroutine(formID, startPosition, startRotation);
            _loadBalancer.AddTaskPriority(loadCoroutine);
        }

        private IEnumerator LoadCellCoroutine(uint formID, Vector3? startPosition,
            Quaternion? startRotation)
        {
            var clearCoroutine = Coroutine.Get(DestroyAndClearEverything(), nameof(DestroyAndClearEverything));
            while (clearCoroutine.MoveNext())
                yield return null;

            ActiveDoorTeleport = null;
            GameState = GameState.Loading;
            _cellManager.LoadCell(formID, startPosition, startRotation,
                () => { GameState = GameState.InGame; });
        }

        public void Update()
        {
            CameraPlanes = GeometryUtility.CalculateFrustumPlanes(MainCamera);
            _cellManager.Update();
            _loadBalancer.RunTasks();
        }

        private IEnumerator DestroyAndClearEverything()
        {
            var cellCoroutine = Coroutine.Get(_cellManager.DestroyAllCells(), nameof(_cellManager.DestroyAllCells));
            while (cellCoroutine.MoveNext())
            {
                yield return null;
            }

            var nifCoroutine = Coroutine.Get(_nifManager.ClearModelCache(), nameof(_nifManager.ClearModelCache));
            while (nifCoroutine.MoveNext())
            {
                yield return null;
            }

            var materialCoroutine = Coroutine.Get(_materialManager.ClearCachedMaterialsAndTextures(),
                nameof(_materialManager.ClearCachedMaterialsAndTextures));
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