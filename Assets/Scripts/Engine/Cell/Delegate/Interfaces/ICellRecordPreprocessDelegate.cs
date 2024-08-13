using System.Collections;
using MasterFile.MasterFileContents;
using MasterFile.MasterFileContents.Records;
using UnityEngine;

namespace Engine.Cell.Delegate.Interfaces
{
    public interface ICellRecordPreprocessDelegate
    {
        public IEnumerator PreprocessRecord(CELL cell, Record record, GameObject parent);
    }
    
    public abstract class CellRecordPreprocessDelegate<T> : ICellRecordPreprocessDelegate where T : Record
    {
        protected abstract IEnumerator PreprocessRecord(CELL cell, T record, GameObject parent);
        
        public IEnumerator PreprocessRecord(CELL cell, Record record, GameObject parent)
        {
            if (record is T recordType)
                return PreprocessRecord(cell, recordType, parent);
            Debug.LogError($"Expected record {typeof(T).Name} but got {record.GetType().Name}");
            return null;
        }
    }
}