using System.Collections;
using MasterFile.MasterFileContents;
using MasterFile.MasterFileContents.Records;
using UnityEngine;

namespace Engine.Cell.Delegate.Interfaces
{
    public interface ICellRecordPreprocessDelegate
    {
        public IEnumerator PreprocessRecord(CELL cell, Record record, GameObject parent, LoadCause loadCause);
    }
    
    public abstract class CellRecordPreprocessDelegate<T> : ICellRecordPreprocessDelegate where T : Record
    {
        protected abstract IEnumerator PreprocessRecord(CELL cell, T record, GameObject parent, LoadCause loadCause);
        
        public IEnumerator PreprocessRecord(CELL cell, Record record, GameObject parent, LoadCause loadCause)
        {
            if (record is T recordType)
                return PreprocessRecord(cell, recordType, parent, loadCause);
            Debug.LogError($"Expected record {typeof(T).Name} but got {record.GetType().Name}");
            return null;
        }
    }
}