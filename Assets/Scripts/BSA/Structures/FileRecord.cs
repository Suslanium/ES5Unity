using System.IO;
using BSA.Structures.Enums;

namespace BSA.Structures
{
    public class FileRecord
    {
        /// <summary>
        /// Hash of the file name (eg: race_sex_menu.xml). Must be all lower case.
        /// </summary>
        public long Hash { get; private set; }
        
        /// <summary>
        /// Size of the file data.
        /// </summary>
        public uint Size { get; private set; }
        
        public bool IsCompressed { get; private set; }
        
        /// <summary>
        /// Offset to raw file data for this folder. Note that an "offset" is offset from file byte zero (start).
        /// </summary>
        public uint Offset { get; private set; }
        
        private FileRecord() {}

        public static FileRecord Parse(BinaryReader binaryReader, Header header)
        {
            var record = new FileRecord
            {
                Hash = binaryReader.ReadInt64(),
                Size = binaryReader.ReadUInt32(),
                Offset = binaryReader.ReadUInt32()
            };
            if (header.ArchiveFlags.Contains(ArchiveFlag.CompressedArchive))
            {
                record.IsCompressed = (record.Size & 0x40000000) == 0;
            }
            else
            {
                record.IsCompressed = (record.Size & 0x40000000) != 0;
            }

            if (record.IsCompressed)
            {
                record.Size -= 4;
            }

            return record;
        }
    }
}