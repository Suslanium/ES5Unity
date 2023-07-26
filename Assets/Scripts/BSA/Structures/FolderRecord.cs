using System.IO;

namespace BSA.Structures
{
    public class FolderRecord
    {
        /// <summary>
        /// Hash of the folder name (eg: menus\chargen). Must be all lower case, and use backslash as directory delimiter(s).
        /// </summary>
        public long Hash { get; private set; }
        
        /// <summary>
        /// Amount of files in this folder.
        /// </summary>
        public uint FileCount { get; private set; }
        
        /// <summary>
        /// Offset to file records for this folder.
        /// </summary>
        public uint Offset { get; private set; }
        
        /// <summary>
        /// Present if folder contents were loaded
        /// </summary>
        public FileRecordBlock Files { get; set; }
        
        private FolderRecord() {}

        public static FolderRecord Parse(BinaryReader binaryReader, Header header)
        {
            var record = new FolderRecord
            {
                Hash = binaryReader.ReadInt64(),
                FileCount = binaryReader.ReadUInt32()
            };
            if (header.Version == 0x69) binaryReader.BaseStream.Seek(4, SeekOrigin.Current);
            record.Offset = binaryReader.ReadUInt32()-header.TotalFileNameLength;
            if (header.Version == 0x69) binaryReader.BaseStream.Seek(4, SeekOrigin.Current);
            return record;
        }
    }
}