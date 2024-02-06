using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MasterFile.MasterFileContents;
using MasterFile.MasterFileContents.Records;

namespace MasterFile
{
    /// <summary>
    /// Mod files (Plugin files) are collections of records, which are further divided into fields. Records themselves are organized into groups.
    /// <para>At the highest grouping level, a plugin file is generally:</para>
    /// <para>- A single TES4 record (plugin information).</para>
    /// <para>- A collection of top groups. </para>
    /// </summary>
    public class ESMasterFile
    {
        public TES4 PluginInfo { get; private set; }
        private Dictionary<uint, long> FormIdToPosition { get; set; } = new();
        private Dictionary<uint, Group> FormIdToParentGroup { get; set; } = new();
        private Dictionary<string, long> TypeToTopGroupPosition { get; set; } = new();
        private Dictionary<string, Dictionary<uint, long>> RecordTypeDictionary { get; set; } = new();
        private readonly BinaryReader _fileReader;
        private readonly Task _initializationTask;
        private readonly Random _random = new();

        public ESMasterFile(BinaryReader fileReader)
        {
            _fileReader = fileReader;
            _initializationTask = Task.Run(() => Initialize(fileReader));
        }

        private void Initialize(BinaryReader fileReader)
        {
            PluginInfo = MasterFileEntry.Parse(fileReader, 0) as TES4;
            Group currentGroup = null;
            while (fileReader.BaseStream.Position < fileReader.BaseStream.Length)
            {
                var entryStartPos = fileReader.BaseStream.Position;
                var entry = MasterFileEntry.ReadHeaderAndSkip(fileReader, fileReader.BaseStream.Position);
                switch (entry)
                {
                    case Record record:
                        FormIdToPosition.Add(record.FormID, entryStartPos);
                        FormIdToParentGroup.Add(record.FormID, currentGroup);
                        if (!RecordTypeDictionary.ContainsKey(record.Type))
                        {
                            RecordTypeDictionary.Add(record.Type,
                                new Dictionary<uint, long> { { record.FormID, entryStartPos } });
                        }
                        else
                        {
                            RecordTypeDictionary[record.Type].Add(record.FormID, entryStartPos);
                        }

                        break;
                    case Group group:
                    {
                        if (group.GroupType == 0)
                        {
                            var recordType = System.Text.Encoding.UTF8.GetString(group.Label);
                            TypeToTopGroupPosition.TryAdd(recordType, entryStartPos);
                        }

                        currentGroup = group;

                        break;
                    }
                }
            }
        }

        /// <summary>
        /// WARNING: If the next object is a group - this will read the entire group including all its records.
        /// </summary>
        private MasterFileEntry ReadNext()
        {
            return MasterFileEntry.Parse(_fileReader, _fileReader.BaseStream.Position);
        }

        public Task<MasterFileEntry> ReadNextTask()
        {
            return Task.Run(() =>
            {
                if (!_initializationTask.IsCompleted)
                    _initializationTask.Wait();
                return ReadNext();
            });
        }

        private Record GetFromFormID(uint formId)
        {
            if (FormIdToPosition.TryGetValue(formId, out var position))
            {
                var record = (Record)MasterFileEntry.Parse(_fileReader, position);
                return record;
            }

            return null;
        }

        public Task<Record> GetFromFormIDTask(uint formId)
        {
            return Task.Run(() =>
            {
                if (!_initializationTask.IsCompleted)
                    _initializationTask.Wait();
                return GetFromFormID(formId);
            });
        }

        private CELL FindCellByEditorID(string editorID)
        {
            editorID += "\0";
            var cellRecordDictionary = RecordTypeDictionary["CELL"];
            return cellRecordDictionary.Keys.Select(formId => (CELL)GetFromFormID(formId))
                .FirstOrDefault(record => record.EditorID == editorID);
        }

        public Task<CELL> FindCellByEditorIDTask(string editorID)
        {
            return Task.Run(() =>
            {
                if (!_initializationTask.IsCompleted)
                    _initializationTask.Wait();
                return FindCellByEditorID(editorID);
            });
        }

        private Record GetRandomRecordOfType(string type)
        {
            var records = RecordTypeDictionary[type];
            var recordPos = records.ElementAt(_random.Next(0, records.Count)).Value;
            var record = (Record)MasterFileEntry.Parse(_fileReader, recordPos);
            return record;
        }

        public Task<Record> GetRandomRecordOfTypeTask(string type)
        {
            return Task.Run(() =>
            {
                if (!_initializationTask.IsCompleted)
                    _initializationTask.Wait();
                return GetRandomRecordOfType(type);
            });
        }

        /// <summary>
        /// For the record stored in the World Children/Cell (Persistent/Temporary) Children/Topic Children Group, find the FormID of their parent WRLD/CELL/DIAL
        /// </summary>
        /// <param name="recordFormID">The formID of the record stored in the group</param>
        /// <returns>The formID of the parent record, or 0 if the record does not exist or is not stored in one of the groups specified above</returns>
        public uint GetParentFormID(uint recordFormID)
        {
            FormIdToParentGroup.TryGetValue(recordFormID, out var parentGroup);
            if (parentGroup == null)
            {
                return 0;
            }

            return parentGroup.GroupType is not 1 and not 6 and not 7 and not 8 and not 9
                ? 0
                : BitConverter.ToUInt32(parentGroup.Label);
        }

        /// <summary>
        /// Call this only when exiting the game
        /// </summary>
        public void Close()
        {
            if (!_initializationTask.IsCompleted)
                _initializationTask.Wait();
            _fileReader.Close();
        }
    }
}