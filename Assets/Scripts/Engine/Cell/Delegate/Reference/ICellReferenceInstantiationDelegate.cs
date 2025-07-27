using System.Collections;
using MasterFile.MasterFileContents;
using MasterFile.MasterFileContents.Records;
using UnityEngine;

namespace Engine.Cell.Delegate.Reference
{
    public interface ICellReferenceInstantiationDelegate
    {
        public bool IsInstantiationApplicable(CELL cell, REFR reference, Record referencedRecord);

        public IEnumerator InstantiateObject(CELL cell, GameObject cellGameObject, REFR reference,
            Record referencedRecord);
    }
}