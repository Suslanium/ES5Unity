using System.Collections;
using System.Collections.Generic;
using Engine.Cell.Delegate.Interfaces;
using Engine.MasterFile;
using MasterFile.MasterFileContents.Records;
using UnityEngine;

namespace Engine.Cell.Delegate.Reference
{
    public class CellReferencePreprocessDelegateManager : CellRecordPreprocessDelegate<REFR>
    {
        private readonly List<ICellReferencePreprocessDelegate> _preprocessDelegates;
        private readonly MasterFileManager _masterFileManager;

        public CellReferencePreprocessDelegateManager(List<ICellReferencePreprocessDelegate> preprocessDelegates,
            MasterFileManager masterFileManager)
        {
            _preprocessDelegates = preprocessDelegates;
            _masterFileManager = masterFileManager;
        }

        protected override IEnumerator PreprocessRecord(CELL cell, REFR record, GameObject parent)
        {
            var referencedRecordTask = _masterFileManager.GetFromFormIDTask(record.BaseObjectReference);
            while (!referencedRecordTask.IsCompleted)
                yield return null;

            var referencedRecord = referencedRecordTask.Result;

            foreach (var preprocessDelegate in _preprocessDelegates)
            {
                if (!preprocessDelegate.IsPreprocessApplicable(cell, record, referencedRecord))
                    continue;

                var preprocessCoroutine =
                    preprocessDelegate.PreprocessObject(cell, parent, record, referencedRecord);
                if (preprocessCoroutine == null) continue;

                while (preprocessCoroutine.MoveNext())
                    yield return null;
            }

            yield return null;
        }
    }
}