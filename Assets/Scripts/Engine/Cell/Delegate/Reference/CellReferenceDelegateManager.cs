using System.Collections;
using System.Collections.Generic;
using Engine.Cell.Delegate.Interfaces;
using Engine.MasterFile;
using MasterFile.MasterFileContents;
using MasterFile.MasterFileContents.Records;
using UnityEngine;
using Coroutine = Engine.Core.Coroutine;

namespace Engine.Cell.Delegate.Reference
{
    public class CellReferenceDelegateManager : ICellRecordPreprocessDelegate, ICellRecordInstantiationDelegate,
        ICellDestroyDelegate
    {
        private readonly List<ICellReferencePreprocessDelegate> _preprocessDelegates;
        private readonly List<ICellReferenceInstantiationDelegate> _instantiationDelegates;
        private readonly MasterFileManager _masterFileManager;
        private readonly Dictionary<uint, Record> _baseObjectCache = new();

        public CellReferenceDelegateManager(List<ICellReferencePreprocessDelegate> preprocessDelegates,
            List<ICellReferenceInstantiationDelegate> instantiationDelegates,
            MasterFileManager masterFileManager)
        {
            _preprocessDelegates = preprocessDelegates;
            _instantiationDelegates = instantiationDelegates;
            _masterFileManager = masterFileManager;
        }

        public IEnumerator PreprocessRecord(CELL cell, Record record, GameObject parent)
        {
            if (record is not REFR reference) yield break;

            if (!_baseObjectCache.TryGetValue(reference.BaseObjectReference, out var referencedRecord))
            {
                var referencedRecordTask = _masterFileManager.GetFromFormIDTask(reference.BaseObjectReference);
                while (!referencedRecordTask.IsCompleted)
                    yield return null;

                referencedRecord = referencedRecordTask.Result;
                _baseObjectCache[reference.BaseObjectReference] = referencedRecord;
            }

            foreach (var preprocessDelegate in _preprocessDelegates)
            {
                if (!preprocessDelegate.IsPreprocessApplicable(cell, reference, referencedRecord))
                    continue;

                var preprocessCoroutine =
                    Coroutine.Get(preprocessDelegate.PreprocessObject(cell, parent, reference, referencedRecord),
                        nameof(preprocessDelegate.PreprocessObject));
                if (preprocessCoroutine == null) continue;

                while (preprocessCoroutine.MoveNext())
                    yield return null;
            }

            yield return null;
        }

        public IEnumerator InstantiateRecord(CELL cell, Record record, GameObject parent)
        {
            if (record is not REFR reference) yield break;

            if (!_baseObjectCache.TryGetValue(reference.BaseObjectReference, out var referencedRecord))
            {
                var referencedRecordTask = _masterFileManager.GetFromFormIDTask(reference.BaseObjectReference);
                while (!referencedRecordTask.IsCompleted)
                    yield return null;

                referencedRecord = referencedRecordTask.Result;
                _baseObjectCache[reference.BaseObjectReference] = referencedRecord;
            }

            var objectInstantiationCoroutine =
                Coroutine.Get(InstantiateCellReference(cell, parent, reference, referencedRecord),
                    nameof(InstantiateCellReference));
            if (objectInstantiationCoroutine == null) yield break;
            while (objectInstantiationCoroutine.MoveNext())
                yield return null;
        }

        private IEnumerator InstantiateCellReference(CELL cell, GameObject parent,
            REFR referenceRecord,
            Record referencedRecord)
        {
            if (referencedRecord == null) yield break;
            foreach (var delegateInstance in _instantiationDelegates)
            {
                if (!delegateInstance.IsInstantiationApplicable(cell, referenceRecord,
                        referencedRecord))
                    continue;

                var instantiationCoroutine =
                    Coroutine.Get(delegateInstance.InstantiateObject(cell, parent, referenceRecord, referencedRecord),
                        nameof(delegateInstance.InstantiateObject));
                if (instantiationCoroutine == null) continue;

                while (instantiationCoroutine.MoveNext())
                    yield return null;
            }
        }

        public IEnumerator OnDestroy()
        {
            _baseObjectCache.Clear();
            yield break;
        }
    }
}