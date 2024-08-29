using System.Collections;
using System.Collections.Generic;
using Engine.Cell.Delegate.Interfaces;
using Engine.MasterFile.Structures;
using MasterFile.MasterFileContents;
using MasterFile.MasterFileContents.Records;
using UnityEngine;
using Coroutine = Engine.Core.Coroutine;

namespace Engine.Cell.Delegate.Reference
{
    public class CellReferenceDelegateManager : ICellRecordPreprocessDelegate, ICellRecordInstantiationDelegate
    {
        private readonly List<ICellReferencePreprocessDelegate> _preprocessDelegates;
        private readonly List<ICellReferenceInstantiationDelegate> _instantiationDelegates;

        public CellReferenceDelegateManager(List<ICellReferencePreprocessDelegate> preprocessDelegates,
            List<ICellReferenceInstantiationDelegate> instantiationDelegates)
        {
            _preprocessDelegates = preprocessDelegates;
            _instantiationDelegates = instantiationDelegates;
        }

        public IEnumerator PreprocessRecord(CellData cellData, Record record, GameObject parent)
        {
            if (record is not REFR reference) yield break;

            if (!cellData.ReferenceBaseObjects.TryGetValue(reference.BaseObjectReference, out var referencedRecord))
            {
                yield break;
            }

            foreach (var preprocessDelegate in _preprocessDelegates)
            {
                if (!preprocessDelegate.IsPreprocessApplicable(cellData.CellRecord, reference, referencedRecord))
                    continue;

                var preprocessCoroutine =
                    Coroutine.Get(
                        preprocessDelegate.PreprocessObject(cellData.CellRecord, parent, reference, referencedRecord),
                        nameof(preprocessDelegate.PreprocessObject));
                if (preprocessCoroutine == null) continue;

                while (preprocessCoroutine.MoveNext())
                    yield return null;
            }

            yield return null;
        }

        public IEnumerator InstantiateRecord(CellData cellData, Record record, GameObject parent)
        {
            if (record is not REFR reference) yield break;

            if (!cellData.ReferenceBaseObjects.TryGetValue(reference.BaseObjectReference, out var referencedRecord))
                yield break;

            var objectInstantiationCoroutine =
                Coroutine.Get(InstantiateCellReference(cellData.CellRecord, parent, reference, referencedRecord),
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
    }
}