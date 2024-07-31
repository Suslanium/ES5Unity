using System.Collections;
using MasterFile.MasterFileContents;
using MasterFile.MasterFileContents.Records;
using UnityEngine;

namespace Engine.Cell.Delegate.Interfaces
{
    public interface ICellReferencePreprocessDelegate
    {
        public bool IsPreprocessApplicable(CELL cell, LoadCause loadCause, REFR reference, Record referencedRecord);
        
        public IEnumerator PreprocessObject(CELL cell, GameObject cellGameObject, LoadCause loadCause, REFR reference,
            Record referencedRecord);
    }
}