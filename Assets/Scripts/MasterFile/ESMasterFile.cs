using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MasterFile.MasterFileContents;
using MasterFile.MasterFileContents.Records;
using UnityEngine;
using Random = System.Random;

namespace MasterFile
{
    public struct ExteriorCellSubBlockID
    {
        public readonly uint WorldSpaceFormID;

        public readonly short BlockX;
        public readonly short BlockY;

        public readonly short SubBlockX;
        public readonly short SubBlockY;

        public ExteriorCellSubBlockID(uint worldSpaceFormID, short blockX, short blockY, short subBlockX,
            short subBlockY)
        {
            WorldSpaceFormID = worldSpaceFormID;
            BlockX = blockX;
            BlockY = blockY;
            SubBlockX = subBlockX;
            SubBlockY = subBlockY;
        }
    }

    /// <summary>
    /// Mod files (Plugin files) are collections of records, which are further divided into fields. Records themselves are organized into groups.
    /// <para>At the highest grouping level, a plugin file is generally:</para>
    /// <para>- A single TES4 record (plugin information).</para>
    /// <para>- A collection of top groups. </para>
    /// </summary>
    public class ESMasterFile
    {
        public TES4 PluginInfo { get; private set; }
        private readonly Dictionary<uint, long> _formIdToPosition = new();
        private readonly Dictionary<uint, Group> _formIdToParentGroup = new();
        private readonly Dictionary<string, long> _typeToTopGroupPosition = new();
        private readonly Dictionary<string, Dictionary<uint, long>> _recordTypeDictionary = new();
        private readonly Dictionary<ExteriorCellSubBlockID, List<uint>> _exteriorCellSubBlockToCellFormIDs = new();
        private readonly Dictionary<uint, long> _worldSpaceFormIDToPersistentCellPosition = new();
        private readonly Dictionary<uint, uint> _cellFormIdToWorldSpaceFormId = new();

        private readonly ConcurrentDictionary<ExteriorCellSubBlockID, Dictionary<Vector2Int, CELL>>
            _loadedExteriorCellSubBlocks = new();

        private readonly BinaryReader _fileReader;
        private readonly Task _initializationTask;
        private readonly Random _random = new(DateTime.Now.Millisecond);

        public ESMasterFile(BinaryReader fileReader)
        {
            _fileReader = fileReader;
            PluginInfo = MasterFileEntry.Parse(fileReader, 0) as TES4;
            _initializationTask = Task.Run(() => Initialize(fileReader));
        }

        private void Initialize(BinaryReader fileReader)
        {
            Stack<(Group, long)> groupStack = new();
            uint currentWorldSpaceFormID = 0;
            short currentCellBlockX = 0;
            short currentCellBlockY = 0;
            ExteriorCellSubBlockID? currentExteriorCellSubBlockID = null;
            while (fileReader.BaseStream.Position < fileReader.BaseStream.Length)
            {
                Group currentGroup = null;
                if (groupStack.Count > 0)
                {
                    var endPos = groupStack.Peek().Item2;
                    while (fileReader.BaseStream.Position >= endPos)
                    {
                        groupStack.Pop();
                        if (groupStack.Count == 0)
                            break;
                        endPos = groupStack.Peek().Item2;
                    }

                    if (groupStack.Count > 0)
                        currentGroup = groupStack.Peek().Item1;
                }

                var entryStartPos = fileReader.BaseStream.Position;
                var entry = MasterFileEntry.ReadHeaderAndSkip(fileReader, fileReader.BaseStream.Position);
                switch (entry)
                {
                    case Record record:
                        _formIdToPosition.Add(record.FormID, entryStartPos);
                        _formIdToParentGroup.Add(record.FormID, currentGroup);

                        if (!_recordTypeDictionary.TryGetValue(record.Type, out var value))
                        {
                            _recordTypeDictionary.Add(record.Type,
                                new Dictionary<uint, long> { { record.FormID, entryStartPos } });
                        }
                        else
                        {
                            value.Add(record.FormID, entryStartPos);
                        }

                        if (record.Type == "WRLD")
                        {
                            currentWorldSpaceFormID = record.FormID;
                        }
                        else if (record.Type == "CELL" &&
                                 currentGroup is { GroupType: 5 } &&
                                 currentExteriorCellSubBlockID != null)
                        {
                            _exteriorCellSubBlockToCellFormIDs[currentExteriorCellSubBlockID.Value].Add(record.FormID);
                            _cellFormIdToWorldSpaceFormId[record.FormID] = currentWorldSpaceFormID;
                        }
                        else if (currentGroup is { GroupType: 1 } &&
                                 record.Type == "CELL")
                        {
                            _worldSpaceFormIDToPersistentCellPosition[currentWorldSpaceFormID] = entryStartPos;
                            _cellFormIdToWorldSpaceFormId[record.FormID] = currentWorldSpaceFormID;
                        }

                        break;
                    case Group group:
                    {
                        switch (group.GroupType)
                        {
                            case 0:
                            {
                                var recordType = System.Text.Encoding.UTF8.GetString(group.Label);
                                _typeToTopGroupPosition.TryAdd(recordType, entryStartPos);
                                break;
                            }
                            case 4:
                                currentCellBlockY = BitConverter.ToInt16(new[] { group.Label[0], group.Label[1] }, 0);
                                currentCellBlockX = BitConverter.ToInt16(new[] { group.Label[2], group.Label[3] }, 0);
                                break;
                            case 5:
                                var currentCellSubBlockY =
                                    BitConverter.ToInt16(new[] { group.Label[0], group.Label[1] }, 0);
                                var currentCellSubBlockX =
                                    BitConverter.ToInt16(new[] { group.Label[2], group.Label[3] }, 0);
                                currentExteriorCellSubBlockID = new ExteriorCellSubBlockID(currentWorldSpaceFormID,
                                    currentCellBlockX, currentCellBlockY, currentCellSubBlockX, currentCellSubBlockY);
                                _exteriorCellSubBlockToCellFormIDs[currentExteriorCellSubBlockID.Value] =
                                    new List<uint>();
                                break;
                        }

                        groupStack.Push((group, _fileReader.BaseStream.Position + group.Size - 24));

                        break;
                    }
                }
            }
        }

        public async Task AwaitInitialization()
        {
            await _initializationTask;
        }

        /// <summary>
        /// CAUTION: If the next object is a group - this will read the entire group including all its records.
        /// CAUTION #2: This should be called only after the master file has been initialized.
        /// If the master file has not been initialized, this function won't work properly.
        /// </summary>
        public MasterFileEntry ReadAfterRecord(Record record)
        {
            if (!_formIdToPosition.TryGetValue(record.FormID, out var position))
            {
                return null;
            }
            else
            {
                lock (_fileReader)
                {
                    MasterFileEntry.ReadHeaderAndSkip(_fileReader, position);
                    return MasterFileEntry.Parse(_fileReader, _fileReader.BaseStream.Position);
                }
            }
        }

        /// <summary>
        /// CAUTION: This should be called only after the master file has been initialized.
        /// If the master file has not been initialized, this function won't work properly.
        /// </summary>
        public Record GetFromFormID(uint formId)
        {
            if (!_formIdToPosition.TryGetValue(formId, out var position)) return null;
            lock (_fileReader)
            {
                var record = (Record)MasterFileEntry.Parse(_fileReader, position);
                return record;
            }
        }

        /// <summary>
        /// Checks if a record with the specified FormID exists in the master file.
        /// CAUTION: This should be called only after the master file has been initialized.
        /// If the master file has not been initialized, this function won't work properly.
        /// </summary>
        public bool RecordExists(uint formId)
        {
            return _formIdToPosition.ContainsKey(formId);
        }

        /// <summary>
        /// CAUTION: This should be called only after the master file has been initialized.
        /// If the master file has not been initialized, this function won't work properly.
        /// </summary>
        public CELL FindCellByEditorID(string editorID)
        {
            editorID += "\0";
            var cellRecordDictionary = _recordTypeDictionary["CELL"];
            return cellRecordDictionary.Keys.Select(formId => (CELL)GetFromFormID(formId))
                .FirstOrDefault(record => record.EditorID == editorID);
        }

        /// <summary>
        /// CAUTION: This should be called only after the master file has been initialized.
        /// If the master file has not been initialized, this function won't work properly.
        /// </summary>
        public Record GetRandomRecordOfType(string type)
        {
            var records = _recordTypeDictionary[type];
            var recordPos = records.ElementAt(_random.Next(0, records.Count)).Value;
            lock (_fileReader)
            {
                var record = (Record)MasterFileEntry.Parse(_fileReader, recordPos);
                return record;
            }
        }

        /// <summary>
        /// CAUTION: This should be called only after the master file has been initialized.
        /// If the master file has not been initialized, this function won't work properly.
        /// </summary>
        public bool ContainsRecordsOfType(string type)
        {
            return _recordTypeDictionary.ContainsKey(type);
        }

        /// <summary>
        /// For the record stored in the World Children/Cell (Persistent/Temporary) Children/Topic Children Group, find the FormID of their parent WRLD/CELL/DIAL
        /// CAUTION: This should be called only after the master file has been initialized.
        /// If the master file has not been initialized, this function won't work properly.
        /// </summary>
        /// <param name="recordFormID">The formID of the record stored in the group</param>
        /// <returns>The formID of the parent record, or 0 if the record does not exist or is not stored in one of the groups specified above</returns>
        public uint GetParentFormID(uint recordFormID)
        {
            _formIdToParentGroup.TryGetValue(recordFormID, out var parentGroup);
            if (parentGroup == null)
                return 0;

            return parentGroup.GroupType is not 1 and not 6 and not 7 and not 8 and not 9
                ? 0
                : BitConverter.ToUInt32(parentGroup.Label);
        }

        /// <summary>
        /// CAUTION: This should be called only after the master file has been initialized.
        /// If the master file has not been initialized, this function won't work properly.
        /// </summary>
        public uint GetWorldSpaceFormID(uint cellFormID)
        {
            _cellFormIdToWorldSpaceFormId.TryGetValue(cellFormID, out var worldSpaceFormID);
            return worldSpaceFormID;
        }

        private Dictionary<Vector2Int, CELL> LoadCellSubBlock(ExteriorCellSubBlockID exteriorCellSubBlockID)
        {
            if (_loadedExteriorCellSubBlocks.TryGetValue(exteriorCellSubBlockID, out var value))
                return value;

            if (!_exteriorCellSubBlockToCellFormIDs.TryGetValue(exteriorCellSubBlockID, out var cellFormIDs))
                return null;

            var cellDictionary = new Dictionary<Vector2Int, CELL>();
            foreach (var cellFormID in cellFormIDs)
            {
                var cellRecord = GetFromFormID(cellFormID);
                if (cellRecord is not CELL cell) continue;
                cellDictionary.Add(new Vector2Int(cell.XGridPosition, cell.YGridPosition), cell);
            }

            _loadedExteriorCellSubBlocks.TryAdd(exteriorCellSubBlockID, cellDictionary);
            return cellDictionary;
        }

        /// <summary>
        /// CAUTION: This should be called only after the master file has been initialized.
        /// If the master file has not been initialized, this function won't work properly.
        /// </summary>
        public CELL GetExteriorCellByGridPosition(uint worldSpaceFormId, short blockX, short blockY, short subBlockX,
            short subBlockY, int xGridPosition, int yGridPosition)
        {
            var exteriorCellSubBlockID =
                new ExteriorCellSubBlockID(worldSpaceFormId, blockX, blockY, subBlockX, subBlockY);
            var cellDictionary = LoadCellSubBlock(exteriorCellSubBlockID);
            if (cellDictionary == null)
                return null;

            cellDictionary.TryGetValue(new Vector2Int(xGridPosition, yGridPosition), out var cell);
            return cell;
        }

        /// <summary>
        /// CAUTION: This should be called only after the master file has been initialized.
        /// If the master file has not been initialized, this function won't work properly.
        /// </summary>
        public CELL GetPersistentWorldSpaceCell(uint worldSpaceFormId)
        {
            if (!_worldSpaceFormIDToPersistentCellPosition.TryGetValue(worldSpaceFormId, out var position))
                return null;

            lock (_fileReader)
            {
                var entry = MasterFileEntry.Parse(_fileReader, position);
                return entry as CELL;  
            }
        }

        /// <summary>
        /// Call this only when exiting the game
        /// </summary>
        public void Close()
        {
            if (!_initializationTask.IsCompleted)
                _initializationTask.Wait();
            lock (_fileReader)
            {
                _fileReader.Close();
            }
        }
    }
}