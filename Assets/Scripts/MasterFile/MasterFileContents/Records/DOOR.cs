using System.IO;

namespace MasterFile.MasterFileContents.Records
{
    public class DOOR : Record
    {
        /// <summary>
        /// Editor id
        /// </summary>
        public string EditorID { get; private set; }

        /// <summary>
        /// World model filename(path).
        /// </summary>
        public string NifModelFilename { get; private set; }
        
        /// <summary>
        /// SNDR formID
        /// </summary>
        public uint OpenSound { get; private set; }
        
        /// <summary>
        /// SNDR formID
        /// </summary>
        public uint CloseSound { get; private set; }
        
        /// <summary>
        /// SNDR formID
        /// </summary>
        public uint LoopSound { get; private set; }
        
        /// <summary>
        /// 0x02 Automatic Door
        /// 0x04 Hidden
        /// 0x08 Minimal Use
        /// 0x10 Sliding Door
        /// 0x20 Do Not Open in Combat Search
        /// </summary>
        public byte Flags { get; private set; }
        
        private DOOR(string type, uint dataSize, uint flag, uint formID, ushort timestamp, ushort versionControlInfo,
            ushort internalRecordVersion, ushort unknownData) : base(type, dataSize, flag, formID, timestamp,
            versionControlInfo, internalRecordVersion, unknownData)
        {
        }
        
        public static DOOR ParseSpecific(Record baseInfo, BinaryReader fileReader, long position)
        {
            var door = new DOOR(baseInfo.Type, baseInfo.DataSize, baseInfo.Flag, baseInfo.FormID, baseInfo.Timestamp,
                baseInfo.VersionControlInfo, baseInfo.InternalRecordVersion, baseInfo.UnknownData);
            while (fileReader.BaseStream.Position < position + baseInfo.DataSize)
            {
                var fieldType = new string(fileReader.ReadChars(4));
                var fieldSize = fileReader.ReadUInt16();
                switch (fieldType)
                {
                    case "EDID":
                        door.EditorID = new string(fileReader.ReadChars(fieldSize));
                        break;
                    case "MODL":
                        door.NifModelFilename = new string(fileReader.ReadChars(fieldSize));
                        break;
                    case "SNAM":
                        door.OpenSound = fileReader.ReadUInt32();
                        break;
                    case "ANAM":
                        door.CloseSound = fileReader.ReadUInt32();
                        break;
                    case "BNAM":
                        door.LoopSound = fileReader.ReadUInt32();
                        break;
                    case "FNAM":
                        door.Flags = fileReader.ReadByte();
                        break;
                    default:
                        fileReader.BaseStream.Seek(fieldSize, SeekOrigin.Current);
                        break;
                }
            }

            return door;
        }
    }
}