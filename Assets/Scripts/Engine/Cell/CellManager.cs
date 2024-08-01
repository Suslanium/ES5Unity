using System;
using System.Collections;
using System.Collections.Generic;
using Engine.Cell.Delegate;
using Engine.Cell.Delegate.Interfaces;
using Engine.Cell.Delegate.Reference;
using Engine.Core;
using Engine.MasterFile;
using Engine.Textures;
using MasterFile.MasterFileContents;
using MasterFile.MasterFileContents.Records;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Engine.Cell
{
    public enum LoadCause
    {
        DoorTeleport,
        Coc,
        OpenWorldLoad
    }

    public class CellInfo
    {
        public GameObject CellGameObject { get; set; }

        public List<IEnumerator> ObjectCreationCoroutines { get; } = new();
    }

    public class CellManager
    {
        private readonly MasterFileManager _masterFileManager;
        private readonly TemporalLoadBalancer _temporalLoadBalancer;
        private readonly List<CellInfo> _cells = new();

        private readonly Dictionary<Type, ICellRecordPreprocessDelegate> _preprocessDelegates;
        private readonly Dictionary<Type, ICellRecordInstantiationDelegate> _instantiationDelegates;
        private readonly List<ICellPostProcessDelegate> _postProcessDelegates;
        private readonly List<ICellDestroyDelegate> _destroyDelegates;

        public CellManager(MasterFileManager masterFileManager, NifManager nifManager, TextureManager textureManager,
            TemporalLoadBalancer temporalLoadBalancer,
            GameEngine gameEngine, GameObject player)
        {
            _masterFileManager = masterFileManager;
            _temporalLoadBalancer = temporalLoadBalancer;

            //TODO replace with DI or something
            var cellLightingDelegate = new CellLightingDelegate(gameEngine, masterFileManager);
            var doorDelegate = new DoorDelegate(nifManager, masterFileManager, gameEngine);
            var lightingObjectDelegate = new LightingObjectDelegate(nifManager);
            var occlusionCullingDelegate = new OcclusionCullingDelegate(player, gameEngine);
            var staticObjectDelegate = new StaticObjectDelegate(nifManager);
            var cocPlayerPositionDelegate = new CocPlayerPositionDelegate(player);
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
            _preprocessDelegates = new Dictionary<Type, ICellRecordPreprocessDelegate>
            {
                {
                    typeof(REFR),
                    new CellReferencePreprocessDelegateManager(referencePreprocessDelegates, masterFileManager)
                },
                {
                    typeof(LAND),
                    new TerrainDelegate(masterFileManager, textureManager)
                }
            };
            _instantiationDelegates = new Dictionary<Type, ICellRecordInstantiationDelegate>
            {
                {
                    typeof(REFR),
                    new CellReferenceInstantiationDelegateManager(referenceInstantiationDelegates, masterFileManager)
                }
            };
            _postProcessDelegates = new List<ICellPostProcessDelegate>
            {
                occlusionCullingDelegate,
                cellLightingDelegate
            };
            _destroyDelegates = new List<ICellDestroyDelegate>
            {
                cellLightingDelegate
            };
        }

        public void LoadCell(string editorID, Action onReadyCallback, bool persistentOnly = false)
        {
            var cellInfo = new CellInfo();
            var creationCoroutine = StartCellLoading(editorID, cellInfo, onReadyCallback, persistentOnly);
            cellInfo.ObjectCreationCoroutines.Add(creationCoroutine);
            _temporalLoadBalancer.AddTask(creationCoroutine);
            _cells.Add(cellInfo);
        }

        public void LoadCell(uint formID, LoadCause loadCause, Action onReadyCallback, bool persistentOnly = false)
        {
            var cellInfo = new CellInfo();
            var creationCoroutine = StartCellLoading(formID, cellInfo, loadCause, onReadyCallback, persistentOnly);
            cellInfo.ObjectCreationCoroutines.Add(creationCoroutine);
            _temporalLoadBalancer.AddTask(creationCoroutine);
            _cells.Add(cellInfo);
        }

        private IEnumerator StartCellLoading(string editorId, CellInfo cellInfo, Action onReadyCallback,
            bool persistentOnly = false)
        {
            var cellTask = _masterFileManager.FindCellByEditorIDTask(editorId);
            while (!cellTask.IsCompleted)
                yield return null;

            var cell = cellTask.Result;

            var cellLoadingCoroutine = LoadCellFromRecord(cell, cellInfo, LoadCause.Coc, persistentOnly);
            while (cellLoadingCoroutine.MoveNext())
                yield return null;
            onReadyCallback();
        }

        private IEnumerator StartCellLoading(uint formId, CellInfo cellInfo, LoadCause loadCause,
            Action onReadyCallback,
            bool persistentOnly = false)
        {
            var cellTask = _masterFileManager.GetFromFormIDTask(formId);
            while (!cellTask.IsCompleted)
                yield return null;

            var cell = (CELL)cellTask.Result;

            var cellLoadingCoroutine = LoadCellFromRecord(cell, cellInfo, loadCause, persistentOnly);
            while (cellLoadingCoroutine.MoveNext())
                yield return null;
            onReadyCallback();
        }

        private IEnumerator LoadCellFromRecord(CELL cell, CellInfo cellInfo, LoadCause loadCause,
            bool persistentOnly = false)
        {
            //if ((cell.CellFlag & 0x0001) == 0)
            //    throw new InvalidDataException("Trying to load exterior cell as interior");

            var childrenTask = _masterFileManager.GetCellDataTask(cell.FormID);
            while (!childrenTask.IsCompleted)
                yield return null;

            var cellChildren = childrenTask.Result;

            yield return null;

            var cellGameObject =
                new GameObject(string.IsNullOrEmpty(cell.EditorID) ? cell.FormID.ToString() : cell.EditorID);
            cellInfo.CellGameObject = cellGameObject;
            cellGameObject.SetActive(false);

            var persistentObjectsInstantiationTask =
                InstantiateCellRecords(cell, cellChildren.PersistentChildren, cellGameObject, loadCause);
            while (persistentObjectsInstantiationTask.MoveNext())
                yield return null;

            if (!persistentOnly)
            {
                var temporaryObjectsInstantiationTask =
                    InstantiateCellRecords(cell, cellChildren.TemporaryChildren, cellGameObject, loadCause);
                while (temporaryObjectsInstantiationTask.MoveNext())
                    yield return null;
            }

            var postProcessTask = PostProcessCell(cell, cellGameObject);
            while (postProcessTask.MoveNext())
                yield return null;
        }

        private IEnumerator PostProcessCell(CELL cell, GameObject cellGameObject)
        {
            cellGameObject.SetActive(true);
            //TODO static batching causes a huge freeze
            StaticBatchingUtility.Combine(cellGameObject);
            yield return null;

            foreach (var delegateInstance in _postProcessDelegates)
            {
                var postProcessCoroutine = delegateInstance.PostProcessCell(cell, cellGameObject);
                if (postProcessCoroutine == null) continue;
                while (postProcessCoroutine.MoveNext())
                    yield return null;
            }
        }

        private IEnumerator InstantiateCellRecords(CELL cell, List<Record> children, GameObject parent,
            LoadCause loadCause)
        {
            foreach (var record in children)
            {
                _preprocessDelegates.TryGetValue(record.GetType(), out var preprocessDelegate);
                var preprocessCoroutine = preprocessDelegate?.PreprocessRecord(cell, record, parent, loadCause);
                if (preprocessCoroutine == null) continue;
                while (preprocessCoroutine.MoveNext())
                    yield return null;
            }

            yield return null;

            foreach (var record in children)
            {
                _instantiationDelegates.TryGetValue(record.GetType(), out var instantiationDelegate);
                var instantiationCoroutine = instantiationDelegate?.InstantiateRecord(cell, record, parent, loadCause);
                if (instantiationCoroutine == null) continue;
                while (instantiationCoroutine.MoveNext())
                    yield return null;
            }
        }

        public IEnumerator DestroyAllCells()
        {
            foreach (var destroyDelegate in _destroyDelegates)
            {
                var destroyCoroutine = destroyDelegate.OnDestroy();
                if (destroyCoroutine == null) continue;
                while (destroyCoroutine.MoveNext())
                    yield return null;
            }

            foreach (var cell in _cells)
            {
                if (cell.CellGameObject != null) Object.Destroy(cell.CellGameObject);
                yield return null;
                foreach (var task in cell.ObjectCreationCoroutines)
                    _temporalLoadBalancer.CancelTask(task);

                yield return null;
            }

            _cells.Clear();
        }
    }
}