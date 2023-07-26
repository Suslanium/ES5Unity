using System.Collections.Generic;
using System.IO;

namespace BSA.Structures
{
    /// <summary>
    /// Block of file records for one folder
    /// </summary>
    public class FileRecordBlock
    {
        /// <summary>
        /// Name of the folder. Only present if Bit 1 (IncludeDirNames) of archiveFlags is set.
        /// </summary>
        public string FolderName { get; private set; }

        /// <summary>
        /// File hash to file record map
        /// </summary>
        public Dictionary<long, FileRecord> Files { get; private set; } = new();
        
        private FileRecordBlock() {}

        public static FileRecordBlock Parse(BinaryReader binaryReader, FolderRecord folderRecord, Header header)
        {
            var fileRecordBlock = new FileRecordBlock();
            binaryReader.BaseStream.Seek(folderRecord.Offset, SeekOrigin.Begin);
            var nameLength = binaryReader.ReadByte();
            fileRecordBlock.FolderName = new string(binaryReader.ReadChars(nameLength));
            for (var i = 0; i < folderRecord.FileCount; i++)
            {
                var fileRecord = FileRecord.Parse(binaryReader, header);
                fileRecordBlock.Files[fileRecord.Hash] = fileRecord;
            }

            return fileRecordBlock;
        }
    }
}