using System;
using System.Collections;
using System.Collections.Generic;
using Engine.Cell.Delegate;
using Engine.Cell.Delegate.Interfaces;
using Engine.Cell.Delegate.Reference;
using Engine.Core;
using Engine.MasterFile;
using Engine.MasterFile.Structures;
using Engine.Textures;
using MasterFile.MasterFileContents.Records;
using NIF.Builder;
using UnityEngine;
using Convert = Engine.Core.Convert;
using Object = UnityEngine.Object;
using Coroutine = Engine.Core.Coroutine;

namespace Engine.Cell
{
    public class CellInfo
    {
        public GameObject CellGameObject { get; set; }

        public IEnumerator ObjectCreationCoroutine = null;

        public bool IsLoaded = false;
    }

    public struct CellPosition
    {
        public readonly Vector2Int GridPosition;
        public readonly Vector2Int SubBlock;
        public readonly Vector2Int Block;

        public CellPosition(Vector2Int gridPosition)
        {
            GridPosition = gridPosition;
            SubBlock = new Vector2Int(
                Mathf.FloorToInt((float)gridPosition.x / Convert.ExteriorSubBlockSideLengthInCells),
                Mathf.FloorToInt((float)gridPosition.y / Convert.ExteriorSubBlockSideLengthInCells));
            Block = new Vector2Int(Mathf.FloorToInt((float)SubBlock.x / Convert.ExteriorBlockSideLengthInSubBlocks),
                Mathf.FloorToInt((float)SubBlock.y / Convert.ExteriorBlockSideLengthInSubBlocks));
        }
    }

    public class CellManager
    {
        private readonly MasterFileManager _masterFileManager;
        private readonly TemporalLoadBalancer _temporalLoadBalancer;
        private readonly PlayerManager _playerManager;
        private readonly GameEngine _gameEngine;
        private readonly List<CellInfo> _cells = new();
        private readonly Dictionary<Vector2Int, CellInfo> _exteriorCells = new();
        private readonly List<IEnumerator> _initialLoadingCoroutines = new();
        private uint _currentWorldSpaceFormID = 0;
        private const int LoadRadius = 2;
        private Vector2Int _lastCellPosition = Vector2Int.one * int.MaxValue;

        private readonly Dictionary<Type, ICellRecordPreprocessDelegate> _preprocessDelegates;
        private readonly Dictionary<Type, ICellRecordInstantiationDelegate> _instantiationDelegates;
        private readonly List<ICellPostProcessDelegate> _postProcessDelegates;
        private readonly List<ICellDestroyDelegate> _destroyDelegates;

        public CellManager(MasterFileManager masterFileManager, NifManager nifManager, TextureManager textureManager,
            TemporalLoadBalancer temporalLoadBalancer,
            GameEngine gameEngine, PlayerManager playerManager)
        {
            _masterFileManager = masterFileManager;
            _temporalLoadBalancer = temporalLoadBalancer;
            _playerManager = playerManager;
            _gameEngine = gameEngine;

            //TODO replace with DI or something
            var cellLightingDelegate = new CellLightingDelegate(gameEngine, masterFileManager);
            var doorDelegate = new DoorDelegate(nifManager, masterFileManager, gameEngine);
            var lightingObjectDelegate = new LightingObjectDelegate(nifManager);
            var occlusionCullingDelegate = new OcclusionCullingDelegate(playerManager, gameEngine);
            var staticObjectDelegate = new StaticObjectDelegate(nifManager);
            var cocPlayerPositionDelegate = new CocPlayerPositionDelegate(playerManager);
            var referencePreprocessDelegates = new List<ICellReferencePreprocessDelegate>
            {
                cocPlayerPositionDelegate,
                occlusionCullingDelegate,
                doorDelegate,
                lightingObjectDelegate,
                staticObjectDelegate
            };
            var referenceInstantiationDelegates = new List<ICellReferenceInstantiationDelegate>
            {
                doorDelegate,
                lightingObjectDelegate,
                staticObjectDelegate
            };
            var referenceDelegateManager =
                new CellReferenceDelegateManager(referencePreprocessDelegates, referenceInstantiationDelegates);
            var terrainDelegate = new TerrainDelegate(masterFileManager, textureManager);
            _preprocessDelegates = new Dictionary<Type, ICellRecordPreprocessDelegate>
            {
                {
                    typeof(REFR),
                    referenceDelegateManager
                },
                {
                    typeof(LAND),
                    terrainDelegate
                }
            };
            _instantiationDelegates = new Dictionary<Type, ICellRecordInstantiationDelegate>
            {
                {
                    typeof(REFR),
                    referenceDelegateManager
                },
                {
                    typeof(LAND),
                    terrainDelegate
                }
            };
            _postProcessDelegates = new List<ICellPostProcessDelegate>
            {
                cocPlayerPositionDelegate,
                occlusionCullingDelegate,
                cellLightingDelegate
            };
            _destroyDelegates = new List<ICellDestroyDelegate>
            {
                cellLightingDelegate,
                cocPlayerPositionDelegate,
                terrainDelegate
            };
        }

        public void LoadCell(string editorID, Vector3? startPos,
            Quaternion? startRot, Action onReadyCallback, bool persistentOnly = false)
        {
            var creationCoroutine = InitializeCellLoading(editorID, startPos, startRot, onReadyCallback,
                persistentOnly);
            _initialLoadingCoroutines.Add(creationCoroutine);
            _temporalLoadBalancer.AddTask(creationCoroutine);
        }

        public void LoadCell(uint formID, Vector3? startPos,
            Quaternion? startRot, Action onReadyCallback, bool persistentOnly = false)
        {
            var creationCoroutine =
                InitializeCellLoading(formID, startPos, startRot, onReadyCallback, persistentOnly);
            _initialLoadingCoroutines.Add(creationCoroutine);
            _temporalLoadBalancer.AddTask(creationCoroutine);
        }

        private static IEnumerator<CellPosition> GetExteriorCellPositions(Vector2Int gridPosition,
            bool fromCenter = false)
        {
            if (fromCenter)
                yield return new CellPosition(gridPosition);

            for (var radius = fromCenter ? 1 : LoadRadius;
                 fromCenter ? radius <= LoadRadius : radius >= 1;
                 radius = fromCenter ? radius + 1 : radius - 1)
            {
                var minCellX = gridPosition.x - radius;
                var maxCellX = gridPosition.x + radius;
                var minCellY = gridPosition.y - radius;
                var maxCellY = gridPosition.y + radius;

                //Horizontal sides
                for (var x = minCellX; x <= maxCellX; x++)
                {
                    yield return new CellPosition(new Vector2Int(x, minCellY));
                    yield return new CellPosition(new Vector2Int(x, maxCellY));
                }

                //Vertical sides
                for (var y = minCellY + 1; y <= maxCellY - 1; y++)
                {
                    yield return new CellPosition(new Vector2Int(minCellX, y));
                    yield return new CellPosition(new Vector2Int(maxCellX, y));
                }
            }

            if (!fromCenter)
                yield return new CellPosition(gridPosition);
        }


        // ReSharper disable Unity.PerformanceAnalysis
        public void Update()
        {
            if (_currentWorldSpaceFormID == 0) return;
            if (_gameEngine.GameState != GameState.InGame) return;
            var gridPosition = _playerManager.WorldPlayerPosition.CellGridPosition;
            if (gridPosition == _lastCellPosition) return;
            var enumerator = GetExteriorCellPositions(gridPosition);
            while (enumerator.MoveNext())
            {
                var cellPos = enumerator.Current;
                if (_exteriorCells.TryGetValue(cellPos.GridPosition, out var cellInfo))
                {
                    if (cellInfo.IsLoaded || cellInfo.ObjectCreationCoroutine == null) continue;
                    _temporalLoadBalancer.Prioritize(cellInfo.ObjectCreationCoroutine);
                }
                else
                {
                    cellInfo = new CellInfo();
                    var cellLoadingCoroutine = LoadExteriorCellAtPosition(cellPos, cellInfo);
                    cellInfo.ObjectCreationCoroutine = cellLoadingCoroutine;
                    _cells.Add(cellInfo);
                    _exteriorCells.Add(cellPos.GridPosition, cellInfo);
                    _temporalLoadBalancer.AddTaskPriority(cellLoadingCoroutine);
                }
            }

            _lastCellPosition = gridPosition;
        }

        private IEnumerator LoadExteriorCellAtPosition(CellPosition cellPosition, CellInfo cellInfo)
        {
            var extCellTask = _masterFileManager.GetExteriorCellDataTask(_currentWorldSpaceFormID, cellPosition);
            while (!extCellTask.IsCompleted)
                yield return null;

            var extCell = extCellTask.Result;
            if (extCell == null) yield break;
            var extCellLoadingCoroutine =
                Coroutine.Get(LoadCellFromData(extCell, cellInfo, () => { }), nameof(LoadCellFromData));
            while (extCellLoadingCoroutine.MoveNext())
                yield return null;
        }

        private IEnumerator InitializeCellLoading(string editorId, Vector3? startPos,
            Quaternion? startRot, Action onReadyCallback, bool persistentOnly = false)
        {
            var cellTask = _masterFileManager.FindCellByEditorIDTask(editorId);
            while (!cellTask.IsCompleted)
                yield return null;

            var cellRecord = cellTask.Result;
            if (cellRecord is null) yield break;

            var cellDataTask = _masterFileManager.GetCellDataTask(cellRecord.FormID);
            while (!cellDataTask.IsCompleted)
                yield return null;

            var cellData = cellDataTask.Result;
            if (cellData == null) yield break;

            var initializationCoroutine = Coroutine.Get(InitializeCellLoading(cellData, startPos, startRot,
                onReadyCallback,
                persistentOnly), nameof(InitializeCellLoading));
            while (initializationCoroutine.MoveNext())
                yield return null;
        }

        private IEnumerator InitializeCellLoading(uint formId, Vector3? startPos,
            Quaternion? startRot, Action onReadyCallback, bool persistentOnly = false)
        {
            var cellTask = _masterFileManager.GetCellDataTask(formId);
            while (!cellTask.IsCompleted)
                yield return null;

            var cellData = cellTask.Result;
            if (cellData == null) yield break;

            var initializationCoroutine = Coroutine.Get(InitializeCellLoading(cellData, startPos, startRot,
                onReadyCallback,
                persistentOnly), nameof(InitializeCellLoading));
            while (initializationCoroutine.MoveNext())
                yield return null;
        }

        private IEnumerator InitializeCellLoading(CellData cellData, Vector3? startPos,
            Quaternion? startRot, Action onReadyCallback, bool persistentOnly = false)
        {
            if ((cellData.CellRecord.CellFlag & 0x0001) == 0)
            {
                //Exterior
                _currentWorldSpaceFormID = _masterFileManager.GetWorldSpaceFormID(cellData.CellRecord.FormID);
                _lastCellPosition = Vector2Int.one * int.MaxValue;

                var persistentCellTask =
                    _masterFileManager.GetWorldSpacePersistentCellDataTask(_currentWorldSpaceFormID);
                while (!persistentCellTask.IsCompleted)
                    yield return null;
                var persistentCellData = persistentCellTask.Result;

                var exteriorCellPositions =
                    GetExteriorCellPositions(persistentCellData.CellRecord.FormID == cellData.CellRecord.FormID
                        ? new WorldSpacePosition(NifUtils.UnityPointToNifPoint(startPos.GetValueOrDefault()))
                            .CellGridPosition
                        : new Vector2Int(cellData.CellRecord.XGridPosition, cellData.CellRecord.YGridPosition), true);

                cellData = persistentCellData;
                //Load the current exterior cells
                while (exteriorCellPositions.MoveNext())
                {
                    var cellPosition = exteriorCellPositions.Current;
                    var extCellTask =
                        _masterFileManager.GetExteriorCellDataTask(_currentWorldSpaceFormID, cellPosition);
                    while (!extCellTask.IsCompleted)
                        yield return null;

                    var extCell = extCellTask.Result;
                    if (extCell == null) continue;
                    var extCellInfo = new CellInfo();
                    var extCellLoadingCoroutine =
                        LoadCellFromData(extCell, extCellInfo, () => { }, persistentOnly);
                    extCellInfo.ObjectCreationCoroutine = extCellLoadingCoroutine;
                    _cells.Add(extCellInfo);
                    _exteriorCells.Add(cellPosition.GridPosition, extCellInfo);
                    _temporalLoadBalancer.AddTask(extCellLoadingCoroutine);
                }
            }
            else
            {
                //Interior
                _currentWorldSpaceFormID = 0;
            }

            var cellInfo = new CellInfo();
            var cellLoadingCoroutine =
                LoadCellFromData(cellData, cellInfo, () =>
                {
                    if (startPos != null)
                        _playerManager.PlayerPosition = startPos.Value;
                    if (startRot != null)
                        _playerManager.PlayerRotation = startRot.Value;

                    onReadyCallback();
                }, persistentOnly);
            cellInfo.ObjectCreationCoroutine = cellLoadingCoroutine;
            _cells.Add(cellInfo);
            _temporalLoadBalancer.AddTask(cellLoadingCoroutine);
        }

        private IEnumerator LoadCellFromData(CellData cellData, CellInfo cellInfo,
            Action onReadyCallback,
            bool persistentOnly = false)
        {
            var cellGameObject =
                new GameObject(string.IsNullOrEmpty(cellData.CellRecord.EditorID)
                    ? cellData.CellRecord.FormID.ToString()
                    : cellData.CellRecord.EditorID);
            cellInfo.CellGameObject = cellGameObject;
            cellGameObject.SetActive(false);

            var persistentObjectsInstantiationTask =
                Coroutine.Get(InstantiateCellRecords(cellData, cellGameObject, true),
                    nameof(InstantiateCellRecords));
            while (persistentObjectsInstantiationTask.MoveNext())
                yield return null;

            if (!persistentOnly)
            {
                var temporaryObjectsInstantiationTask =
                    Coroutine.Get(
                        InstantiateCellRecords(cellData, cellGameObject, false),
                        nameof(InstantiateCellRecords));
                while (temporaryObjectsInstantiationTask.MoveNext())
                    yield return null;
            }

            var postProcessTask = Coroutine.Get(PostProcessCell(cellData.CellRecord, cellGameObject),
                nameof(PostProcessCell));
            while (postProcessTask.MoveNext())
                yield return null;

            cellInfo.IsLoaded = true;
            onReadyCallback();
        }

        private IEnumerator PostProcessCell(CELL cell, GameObject cellGameObject)
        {
            cellGameObject.SetActive(true);
            //TODO static batching causes a huge freeze
            //(Only doing during loading screens so that open world loading doesn't freeze the game)
            if (_gameEngine.GameState != GameState.InGame)
                StaticBatchingUtility.Combine(cellGameObject);
            yield return null;

            foreach (var delegateInstance in _postProcessDelegates)
            {
                var postProcessCoroutine = Coroutine.Get(delegateInstance.PostProcessCell(cell, cellGameObject),
                    nameof(delegateInstance.PostProcessCell));
                if (postProcessCoroutine == null) continue;
                while (postProcessCoroutine.MoveNext())
                    yield return null;
            }
        }

        private IEnumerator InstantiateCellRecords(CellData cellData, GameObject parent, bool persistent)
        {
            foreach (var record in persistent ? cellData.PersistentChildren : cellData.TemporaryChildren)
            {
                _preprocessDelegates.TryGetValue(record.GetType(), out var preprocessDelegate);
                var preprocessCoroutine = Coroutine.Get(preprocessDelegate?.PreprocessRecord(cellData, record, parent),
                    nameof(ICellRecordPreprocessDelegate.PreprocessRecord));
                if (preprocessCoroutine == null) continue;
                while (preprocessCoroutine.MoveNext())
                    yield return null;
            }

            yield return null;

            foreach (var record in persistent ? cellData.PersistentChildren : cellData.TemporaryChildren)
            {
                _instantiationDelegates.TryGetValue(record.GetType(), out var instantiationDelegate);
                var instantiationCoroutine =
                    Coroutine.Get(instantiationDelegate?.InstantiateRecord(cellData, record, parent),
                        nameof(ICellRecordInstantiationDelegate.InstantiateRecord));
                if (instantiationCoroutine == null) continue;
                while (instantiationCoroutine.MoveNext())
                    yield return null;
            }
        }

        public IEnumerator DestroyAllCells()
        {
            foreach (var destroyDelegate in _destroyDelegates)
            {
                var destroyCoroutine = Coroutine.Get(destroyDelegate.OnDestroy(), nameof(destroyDelegate.OnDestroy));
                if (destroyCoroutine == null) continue;
                while (destroyCoroutine.MoveNext())
                    yield return null;
            }

            foreach (var coroutine in _initialLoadingCoroutines)
            {
                _temporalLoadBalancer.CancelTask(coroutine);
                yield return null;
            }

            _initialLoadingCoroutines.Clear();

            foreach (var cell in _cells)
            {
                if (cell.CellGameObject != null) Object.Destroy(cell.CellGameObject);
                yield return null;
                _temporalLoadBalancer.CancelTask(cell.ObjectCreationCoroutine);
                yield return null;
            }

            _cells.Clear();
            _exteriorCells.Clear();
        }
    }
}