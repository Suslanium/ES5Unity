using System.Collections;
using Engine.MasterFile.Structures;
using MasterFile.MasterFileContents;
using UnityEngine;
using Logger = Engine.Core.Logger;
using Coroutine = Engine.Core.Coroutine;

namespace Engine.Cell.Delegate.Interfaces
{
    public interface ICellRecordPreprocessDelegate
    {
        public IEnumerator PreprocessRecord(CellData cellData, Record record, GameObject parent);
    }
    
    public abstract class CellRecordPreprocessDelegate<T> : ICellRecordPreprocessDelegate where T : Record
    {
        protected abstract IEnumerator PreprocessRecord(CellData cellData, T record, GameObject parent);
        
        public IEnumerator PreprocessRecord(CellData cellData, Record record, GameObject parent)
        {
            if (record is T recordType)
                return Coroutine.Get(PreprocessRecord(cellData, recordType, parent), nameof(PreprocessRecord));
            Logger.LogError($"Expected record {typeof(T).Name} but got {record.GetType().Name}");
            return null;
        }
    }
}