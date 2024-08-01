using System.Collections;
using MasterFile.MasterFileContents;
using MasterFile.MasterFileContents.Records;
using UnityEngine;

namespace Engine.Cell.Delegate.Interfaces
{
    public interface ICellRecordInstantiationDelegate
    {
        public IEnumerator InstantiateRecord(CELL cell, Record record, GameObject parent, LoadCause loadCause);
    }
    
    public abstract class CellRecordInstantiationDelegate<T> : ICellRecordInstantiationDelegate where T : Record
    {
        protected abstract IEnumerator InstantiateRecord(CELL cell, T record, GameObject parent, LoadCause loadCause);
        
        public IEnumerator InstantiateRecord(CELL cell, Record record, GameObject parent, LoadCause loadCause)
        {
            if (record is T recordType)
                return InstantiateRecord(cell, recordType, parent, loadCause);
            Debug.LogError($"Expected record {typeof(T).Name} but got {record.GetType().Name}");
            return null;
        }
    }
}