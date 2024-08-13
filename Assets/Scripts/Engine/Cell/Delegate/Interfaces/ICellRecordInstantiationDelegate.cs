using System.Collections;
using MasterFile.MasterFileContents;
using MasterFile.MasterFileContents.Records;
using UnityEngine;

namespace Engine.Cell.Delegate.Interfaces
{
    public interface ICellRecordInstantiationDelegate
    {
        public IEnumerator InstantiateRecord(CELL cell, Record record, GameObject parent);
    }
    
    public abstract class CellRecordInstantiationDelegate<T> : ICellRecordInstantiationDelegate where T : Record
    {
        protected abstract IEnumerator InstantiateRecord(CELL cell, T record, GameObject parent);
        
        public IEnumerator InstantiateRecord(CELL cell, Record record, GameObject parent)
        {
            if (record is T recordType)
                return InstantiateRecord(cell, recordType, parent);
            Debug.LogError($"Expected record {typeof(T).Name} but got {record.GetType().Name}");
            return null;
        }
    }
}