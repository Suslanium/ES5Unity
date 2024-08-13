using System.Collections;
using MasterFile.MasterFileContents;
using MasterFile.MasterFileContents.Records;
using UnityEngine;

namespace Engine.Cell.Delegate.Reference
{
    public interface ICellReferencePreprocessDelegate
    {
        public bool IsPreprocessApplicable(CELL cell, REFR reference, Record referencedRecord);

        public IEnumerator PreprocessObject(CELL cell, GameObject cellGameObject, REFR reference,
            Record referencedRecord);
    }
}