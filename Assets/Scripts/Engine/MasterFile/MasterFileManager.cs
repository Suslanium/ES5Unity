using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Engine.Cell;
using MasterFile;
using MasterFile.MasterFileContents;
using MasterFile.MasterFileContents.Records;
using Unity.VisualScripting;
using Random = System.Random;
using Logger = Engine.Core.Logger;

namespace Engine.MasterFile
{
    public class MasterFileManager
    {
        private readonly Dictionary<string, ESMasterFile> _masterFiles = new();
        private readonly List<string> _reverseLoadOrder;
        private readonly Random _random = new(DateTime.Now.Millisecond);
        private bool _masterFilesAreInitialized;

        /// <summary>
        /// Constructor for MasterFileManager. Please note that the order of master files is important.
        /// </summary>
        public MasterFileManager(List<string> masterFilePaths)
        {
            _reverseLoadOrder = new List<string>();
            foreach (var masterFilePath in masterFilePaths)
            {
                var masterFile = new ESMasterFile(new BinaryReader(File.Open(masterFilePath, FileMode.Open)));
                var masterFileName = Path.GetFileName(masterFilePath);

                // Check if all master files are present
                var missingMasters = masterFile.PluginInfo.MasterFiles.Where(master =>
                    !_masterFiles.ContainsKey(master.TrimEnd('\0'))).ToList();
                if (missingMasters.Count > 0)
                {
                    throw new FileNotFoundException(
                        $"Masterfile(s) {missingMasters.ToSeparatedString(",")} " +
                        $"required for {masterFileName} not found. Potentially incorrect load order.");
                }

                _masterFiles.Add(masterFileName, masterFile);
                _reverseLoadOrder.Add(masterFileName);
            }

            _reverseLoadOrder.Reverse();
        }

        /// <summary>
        /// Awaits the initialization of all master files.
        /// All the tasks that work on the master files should await this function before proceeding.
        /// </summary>
        private async Task AwaitInitialization()
        {
            var initializationTasks =
                _masterFiles.Values.Select(masterFile => masterFile.AwaitInitialization()).ToList();
            await Task.WhenAll(initializationTasks);
        }

        public Task<Structures.CellData> GetWorldSpacePersistentCellDataTask(uint worldSpaceFormID)
        {
            return Task.Run(async () =>
            {
                if (!_masterFilesAreInitialized)
                {
                    await AwaitInitialization();
                    _masterFilesAreInitialized = true;
                }

                List<(ESMasterFile, CELL)> cellRecords = new();
                CELL cellRecord = null;

                foreach (var masterFile in _reverseLoadOrder.Select(fileName => _masterFiles[fileName]).Reverse())
                {
                    var currentCellRecord = masterFile.GetPersistentWorldSpaceCell(worldSpaceFormID);
                    if (currentCellRecord == null) continue;
                    cellRecords.Add((masterFile, currentCellRecord));
                    cellRecord = currentCellRecord;
                }

                if (cellRecord == null) return null;

                var persistentChildren = new Dictionary<uint, Record>();
                var temporaryChildren = new Dictionary<uint, Record>();
                var baseObjects = new Dictionary<uint, Record>();

                foreach (var (masterFile, currentCellRecord) in cellRecords)
                {
                    var fileData = masterFile.ReadAfterRecord(currentCellRecord);

                    if (fileData is not Group { GroupType: 6 } childrenGroup)
                    {
                        Logger.LogWarning("Cell children group not found");
                        continue;
                    }

                    foreach (var subGroup in childrenGroup.GroupData)
                    {
                        if (subGroup is not Group group) continue;
                        if (group.GroupType != 8 && group.GroupType != 9) continue;

                        foreach (var entry in group.GroupData)
                        {
                            if (entry is not Record record) continue;
                            if (group.GroupType == 8) persistentChildren[record.FormID] = record;
                            else temporaryChildren[record.FormID] = record;

                            if (record is REFR reference && reference.BaseObjectReference != 0 &&
                                !baseObjects.ContainsKey(reference.BaseObjectReference))
                            {
                                baseObjects[reference.BaseObjectReference] =
                                    GetFromFormID(reference.BaseObjectReference);
                            }
                        }
                    }
                }

                return new Structures.CellData(persistentChildren.Values.ToList(), temporaryChildren.Values.ToList(),
                    baseObjects,
                    cellRecord);
            });
        }

        public Task<Structures.CellData> GetExteriorCellDataTask(uint worldSpaceFormID, CellPosition position)
        {
            return Task.Run(async () =>
            {
                if (!_masterFilesAreInitialized)
                {
                    await AwaitInitialization();
                    _masterFilesAreInitialized = true;
                }

                List<(ESMasterFile, CELL)> cellRecords = new();
                CELL cellRecord = null;

                foreach (var masterFile in _reverseLoadOrder.Select(fileName => _masterFiles[fileName]).Reverse())
                {
                    var currentCellRecord = masterFile.GetExteriorCellByGridPosition(worldSpaceFormID,
                        (short)position.Block.x,
                        (short)position.Block.y, (short)position.SubBlock.x, (short)position.SubBlock.y,
                        position.GridPosition.x,
                        position.GridPosition.y);
                    if (currentCellRecord == null) continue;
                    cellRecords.Add((masterFile, currentCellRecord));
                    cellRecord = currentCellRecord;
                }

                if (cellRecord == null) return null;

                var persistentChildren = new Dictionary<uint, Record>();
                var temporaryChildren = new Dictionary<uint, Record>();
                var baseObjects = new Dictionary<uint, Record>();

                foreach (var (masterFile, currentCellRecord) in cellRecords)
                {
                    var fileData = masterFile.ReadAfterRecord(currentCellRecord);

                    if (fileData is not Group { GroupType: 6 } childrenGroup)
                    {
                        Logger.LogWarning("Cell children group not found");
                        continue;
                    }

                    foreach (var subGroup in childrenGroup.GroupData)
                    {
                        if (subGroup is not Group group) continue;
                        if (group.GroupType != 8 && group.GroupType != 9) continue;

                        foreach (var entry in group.GroupData)
                        {
                            if (entry is not Record record) continue;
                            if (group.GroupType == 8) persistentChildren[record.FormID] = record;
                            else temporaryChildren[record.FormID] = record;

                            if (record is REFR reference && reference.BaseObjectReference != 0 &&
                                !baseObjects.ContainsKey(reference.BaseObjectReference))
                            {
                                baseObjects[reference.BaseObjectReference] =
                                    GetFromFormID(reference.BaseObjectReference);
                            }
                        }
                    }
                }

                return new Structures.CellData(persistentChildren.Values.ToList(), temporaryChildren.Values.ToList(),
                    baseObjects,
                    cellRecord);
            });
        }

        public Task<Structures.CellData> GetCellDataTask(uint cellFormID)
        {
            return Task.Run(async () =>
            {
                if (!_masterFilesAreInitialized)
                {
                    await AwaitInitialization();
                    _masterFilesAreInitialized = true;
                }

                var masterFiles = _reverseLoadOrder.Select(fileName => _masterFiles[fileName])
                    .Where(masterFile => masterFile.RecordExists(cellFormID)).Reverse().ToList();

                CELL cellRecord = null;
                var persistentChildren = new Dictionary<uint, Record>();
                var temporaryChildren = new Dictionary<uint, Record>();
                var baseObjects = new Dictionary<uint, Record>();

                foreach (var masterFile in masterFiles)
                {
                    var cell = masterFile.GetFromFormID(cellFormID);
                    if (cell is not CELL currentCellRecord) continue;
                    cellRecord = currentCellRecord;
                    var fileData = masterFile.ReadAfterRecord(cell);

                    if (fileData is not Group { GroupType: 6 } childrenGroup)
                    {
                        Logger.LogWarning("Cell children group not found");
                        continue;
                    }

                    foreach (var subGroup in childrenGroup.GroupData)
                    {
                        if (subGroup is not Group group) continue;
                        if (group.GroupType != 8 && group.GroupType != 9) continue;

                        foreach (var entry in group.GroupData)
                        {
                            if (entry is not Record record) continue;
                            if (group.GroupType == 8) persistentChildren[record.FormID] = record;
                            else temporaryChildren[record.FormID] = record;

                            if (record is REFR reference && reference.BaseObjectReference != 0 &&
                                !baseObjects.ContainsKey(reference.BaseObjectReference))
                            {
                                baseObjects[reference.BaseObjectReference] =
                                    GetFromFormID(reference.BaseObjectReference);
                            }
                        }
                    }
                }

                return cellRecord == null
                    ? null
                    : new Structures.CellData(persistentChildren.Values.ToList(), temporaryChildren.Values.ToList(),
                        baseObjects,
                        cellRecord);
            });
        }

        /// <summary>
        /// Finds a record by its form ID.
        /// If records with the same IDs are present
        /// in multiple master files,
        /// this function picks the record from a master file
        /// with the lowest place in the load order.
        /// </summary>
        public Task<Record> GetFromFormIDTask(uint formId)
        {
            return Task.Run(() => GetFromFormID(formId));
        }

        /// <summary>
        /// BLOCKING FUNCTION, DO NOT CALL FROM MAIN THREAD
        /// Finds a record by its form ID.
        /// If records with the same IDs are present
        /// in multiple master files,
        /// this function picks the record from a master file
        /// with the lowest place in the load order.
        /// </summary>
        public Record GetFromFormID(uint formId)
        {
            if (!_masterFilesAreInitialized)
            {
                AwaitInitialization().Wait();
                _masterFilesAreInitialized = true;
            }

            var masterFile =
                _reverseLoadOrder.FirstOrDefault(fileName => _masterFiles[fileName].RecordExists(formId));
            if (masterFile == null) return null;

            var record = _masterFiles[masterFile].GetFromFormID(formId);
            return record;
        }

        /// <summary>
        /// Finds a CELL record by its editor ID.
        /// If records with the same IDs are present
        /// in multiple master files,
        /// this function picks the record from a master file
        /// with the lowest place in the load order.
        /// CAUTION: This function is potentially really slow
        /// since it theoretically loads every CELL record.
        /// </summary>
        public Task<CELL> FindCellByEditorIDTask(string editorID)
        {
            return Task.Run(async () =>
            {
                if (!_masterFilesAreInitialized)
                {
                    await AwaitInitialization();
                    _masterFilesAreInitialized = true;
                }

                return _reverseLoadOrder
                    .Select(masterFileName => _masterFiles[masterFileName].FindCellByEditorID(editorID))
                    .FirstOrDefault(cell => cell != null);
            });
        }

        public Task<Record> GetRandomRecordOfTypeTask(string type)
        {
            return Task.Run(async () =>
            {
                if (!_masterFilesAreInitialized)
                {
                    await AwaitInitialization();
                    _masterFilesAreInitialized = true;
                }

                var suitableMasterFiles = _reverseLoadOrder.Where(fileName =>
                        _masterFiles[fileName].ContainsRecordsOfType(type)).Select(fileName => _masterFiles[fileName])
                    .ToList();

                var randomMasterFile = suitableMasterFiles.ElementAt(_random.Next(0, suitableMasterFiles.Count));
                return randomMasterFile?.GetRandomRecordOfType(type);
            });
        }

        /// <summary>
        /// For the record stored in the World Children/Cell (Persistent/Temporary)
        /// Children/Topic Children Group, find the FormID of their parent WRLD/CELL/DIAL
        /// </summary>
        /// <param name="recordFormID">The formID of the record stored in the group</param>
        /// <returns>The formID of the parent record, or 0 if the record does not
        /// exist or is not stored in one of the groups specified above</returns>
        public uint GetParentFormID(uint recordFormID)
        {
            if (!_masterFilesAreInitialized)
            {
                AwaitInitialization().Wait();
                _masterFilesAreInitialized = true;
            }

            var masterFile = _reverseLoadOrder.Where(fileName => _masterFiles[fileName].RecordExists(recordFormID))
                .Select(fileName => _masterFiles[fileName]).FirstOrDefault();

            return masterFile?.GetParentFormID(recordFormID) ?? 0;
        }

        public uint GetWorldSpaceFormID(uint cellFormID)
        {
            if (!_masterFilesAreInitialized)
            {
                AwaitInitialization().Wait();
                _masterFilesAreInitialized = true;
            }

            var masterFile = _reverseLoadOrder.Where(fileName => _masterFiles[fileName].RecordExists(cellFormID))
                .Select(fileName => _masterFiles[fileName]).FirstOrDefault();

            return masterFile?.GetWorldSpaceFormID(cellFormID) ?? 0;
        }

        public void Close()
        {
            foreach (var masterFile in _masterFiles.Values)
            {
                masterFile.Close();
            }
        }
    }
}