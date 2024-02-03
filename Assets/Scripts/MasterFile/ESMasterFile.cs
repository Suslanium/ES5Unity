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
        private Dictionary<string, long> TypeToTopGroupPosition { get; set; } = new();
        private Dictionary<string, Dictionary<uint, long>> RecordTypeDictionary { get; set; } = new();
        private readonly BinaryReader _fileReader;
        private readonly Task _initializationTask;

        public ESMasterFile(BinaryReader fileReader)
        {
            _fileReader = fileReader;
            _initializationTask = Task.Run(() => Initialize(fileReader));
        }

        private void Initialize(BinaryReader fileReader)
        {
            PluginInfo = MasterFileEntry.Parse(fileReader, 0) as TES4;
            while (fileReader.BaseStream.Position < fileReader.BaseStream.Length)
            {
                var entryStartPos = fileReader.BaseStream.Position;
                var entry = MasterFileEntry.ReadHeaderAndSkip(fileReader, fileReader.BaseStream.Position);
                switch (entry)
                {
                    case Record record:
                        FormIdToPosition.Add(record.FormID, entryStartPos);
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