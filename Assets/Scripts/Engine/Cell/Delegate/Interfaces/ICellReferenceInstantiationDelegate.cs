using System.Collections;
using MasterFile.MasterFileContents;
using MasterFile.MasterFileContents.Records;
using UnityEngine;

namespace Engine.Cell.Delegate.Interfaces
{
    public interface ICellReferenceInstantiationDelegate
    {
        public bool IsInstantiationApplicable(CELL cell, LoadCause loadCause, REFR reference, Record referencedRecord);
        
        public IEnumerator InstantiateObject(CELL cell, GameObject cellGameObject, LoadCause loadCause, REFR reference,
            Record referencedRecord);
    }
}