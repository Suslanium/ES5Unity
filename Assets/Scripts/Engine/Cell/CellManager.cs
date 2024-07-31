using System;
using System.Collections;
using System.Collections.Generic;
using Engine.Cell.Delegate;
using Engine.Cell.Delegate.Interfaces;
using Engine.MasterFile;
using Engine.Utils;
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

        private readonly List<ICellReferencePreprocessDelegate> _preprocessDelegates;
        private readonly List<ICellReferenceInstantiationDelegate> _instantiationDelegates;
        private readonly List<ICellPostProcessDelegate> _postProcessDelegates;
        private readonly List<ICellDestroyDelegate> _destroyDelegates;

        public CellManager(MasterFileManager masterFileManager, NifManager nifManager,
            TemporalLoadBalancer temporalLoadBalancer,
            GameEngine gameEngine, GameObject player)
        {
            _masterFileManager = masterFileManager;
            _temporalLoadBalancer = temporalLoadBalancer;

            var cellLightingDelegate = new CellLightingDelegate(gameEngine, masterFileManager);
            var doorDelegate = new DoorDelegate(nifManager, masterFileManager, gameEngine);
            var lightingObjectDelegate = new LightingObjectDelegate(nifManager);
            var occlusionCullingDelegate = new OcclusionCullingDelegate(player, gameEngine);
            var staticObjectDelegate = new StaticObjectDelegate(nifManager);
            var cocPlayerPositionDelegate = new CocPlayerPositionDelegate(player);
            _preprocessDelegates = new List<ICellReferencePreprocessDelegate>
            {
                cocPlayerPositionDelegate,
                occlusionCullingDelegate,
                doorDelegate,
                lightingObjectDelegate,
                staticObjectDelegate
            };
            _instantiationDelegates = new List<ICellReferenceInstantiationDelegate>
            {
                doorDelegate,
                lightingObjectDelegate,
                staticObjectDelegate
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
                InstantiateCellReferences(cell, cellChildren.PersistentChildren, cellGameObject, loadCause);
            while (persistentObjectsInstantiationTask.MoveNext())
                yield return null;

            if (!persistentOnly)
            {
                var temporaryObjectsInstantiationTask =
                    InstantiateCellReferences(cell, cellChildren.TemporaryChildren, cellGameObject, loadCause);
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

        private IEnumerator InstantiateCellReferences(CELL cell, List<Record> children, GameObject parent,
            LoadCause loadCause)
        {
            foreach (var record in children)
            {
                if (record is not REFR reference) continue;
                var referencedRecordTask = _masterFileManager.GetFromFormIDTask(reference.BaseObjectReference);
                while (!referencedRecordTask.IsCompleted)
                    yield return null;

                var referencedRecord = referencedRecordTask.Result;

                foreach (var preprocessDelegate in _preprocessDelegates)
                {
                    if (!preprocessDelegate.IsPreprocessApplicable(cell, loadCause, reference, referencedRecord))
                        continue;

                    var preprocessCoroutine =
                        preprocessDelegate.PreprocessObject(cell, parent, loadCause, reference, referencedRecord);
                    if (preprocessCoroutine == null) continue;

                    while (preprocessCoroutine.MoveNext())
                        yield return null;
                }

                yield return null;
            }

            yield return null;

            foreach (var record in children)
            {
                if (record is not REFR reference) continue;
                var referencedRecordTask = _masterFileManager.GetFromFormIDTask(reference.BaseObjectReference);
                while (!referencedRecordTask.IsCompleted)
                    yield return null;

                var referencedRecord = referencedRecordTask.Result;
                var objectInstantiationCoroutine =
                    InstantiateCellObject(cell, loadCause, parent, reference, referencedRecord);
                if (objectInstantiationCoroutine == null) continue;
                while (objectInstantiationCoroutine.MoveNext())
                    yield return null;
            }
        }

        private IEnumerator InstantiateCellObject(CELL cell, LoadCause loadCause, GameObject parent,
            REFR referenceRecord,
            Record referencedRecord)
        {
            if (referencedRecord == null) yield break;
            foreach (var delegateInstance in _instantiationDelegates)
            {
                if (!delegateInstance.IsInstantiationApplicable(cell, loadCause, referenceRecord,
                        referencedRecord))
                    continue;

                var instantiationCoroutine = delegateInstance.InstantiateObject(cell, parent,
                    loadCause, referenceRecord, referencedRecord);
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