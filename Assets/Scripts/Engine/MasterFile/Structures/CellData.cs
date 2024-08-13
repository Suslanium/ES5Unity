using System.Collections.Generic;
using MasterFile.MasterFileContents;
using MasterFile.MasterFileContents.Records;

namespace Engine.MasterFile.Structures
{
    public class CellData
    {
        public readonly List<Record> PersistentChildren;
        
        public readonly List<Record> TemporaryChildren;

        public readonly CELL CellRecord;
        
        public CellData(List<Record> persistentChildren, List<Record> temporaryChildren, CELL cellRecord)
        {
            PersistentChildren = persistentChildren;
            TemporaryChildren = temporaryChildren;
            CellRecord = cellRecord;
        }
    }
}