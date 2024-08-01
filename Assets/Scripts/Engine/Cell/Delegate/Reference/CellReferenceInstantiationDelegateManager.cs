using System.Collections;
using System.Collections.Generic;
using Engine.Cell.Delegate.Interfaces;
using Engine.MasterFile;
using MasterFile.MasterFileContents;
using MasterFile.MasterFileContents.Records;
using UnityEngine;

namespace Engine.Cell.Delegate.Reference
{
    public class CellReferenceInstantiationDelegateManager : CellRecordInstantiationDelegate<REFR>
    {
        private readonly List<ICellReferenceInstantiationDelegate> _instantiationDelegates;
        private readonly MasterFileManager _masterFileManager;

        public CellReferenceInstantiationDelegateManager(
            List<ICellReferenceInstantiationDelegate> instantiationDelegates,
            MasterFileManager masterFileManager)
        {
            _instantiationDelegates = instantiationDelegates;
            _masterFileManager = masterFileManager;
        }

        protected override IEnumerator InstantiateRecord(CELL cell, REFR record, GameObject parent, LoadCause loadCause)
        {
            var referencedRecordTask = _masterFileManager.GetFromFormIDTask(record.BaseObjectReference);
            while (!referencedRecordTask.IsCompleted)
                yield return null;

            var referencedRecord = referencedRecordTask.Result;
            var objectInstantiationCoroutine =
                InstantiateCellReference(cell, loadCause, parent, record, referencedRecord);
            if (objectInstantiationCoroutine == null) yield break;
            while (objectInstantiationCoroutine.MoveNext())
                yield return null;
        }

        private IEnumerator InstantiateCellReference(CELL cell, LoadCause loadCause, GameObject parent,
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
    }
}