using System.IO;

namespace MasterFile.MasterFileContents.Records
{
    /// <summary>
    /// LSCR are loading screens.
    /// </summary>
    public class LSCR : Record
    {
        public string EditorID { get; private set; }
        
        public uint StaticNifFormID { get; private set; }
        
        public float InitialScale { get; private set; }
        
        /// <summary>
        /// X, Y, and Z rotation
        /// </summary>
        public short[] InitialRotation { get; private set; }
        
        /// <summary>
        /// Min and Max rotation
        /// </summary>
        public short[] RotationOffsetConstraints { get; private set; }
        
        public float[] InitialTranslation { get; private set; }
        
        private LSCR(string type, uint dataSize, uint flag, uint formID, ushort timestamp, ushort versionControlInfo,
            ushort internalRecordVersion, ushort unknownData) : base(type, dataSize, flag, formID, timestamp,
            versionControlInfo, internalRecordVersion, unknownData)
        {
        }

        public static LSCR ParseSpecific(Record baseInfo, BinaryReader fileReader, long position)
        {
            var lscr = new LSCR(baseInfo.Type, baseInfo.DataSize, baseInfo.Flag, baseInfo.FormID, baseInfo.Timestamp,
                baseInfo.VersionControlInfo, baseInfo.InternalRecordVersion, baseInfo.UnknownData);
            while (fileReader.BaseStream.Position < position + baseInfo.DataSize)
            {
                var fieldType = new string(fileReader.ReadChars(4));
                var fieldSize = fileReader.ReadUInt16();
                switch (fieldType)
                {
                    case "EDID":
                        lscr.EditorID = new string(fileReader.ReadChars(fieldSize));
                        break;
                    case "NNAM":
                        lscr.StaticNifFormID = fileReader.ReadUInt32();
                        break;
                    case "SNAM":
                        lscr.InitialScale = fileReader.ReadSingle();
                        break;
                    case "RNAM":
                        lscr.InitialRotation = new[]
                        {
                            fileReader.ReadInt16(),
                            fileReader.ReadInt16(),
                            fileReader.ReadInt16()
                        };
                        break;
                    case "ONAM":
                        lscr.RotationOffsetConstraints = new[]
                        {
                            fileReader.ReadInt16(),
                            fileReader.ReadInt16()
                        };
                        break;
                    case "XNAM":
                        lscr.InitialTranslation = new[]
                        {
                            fileReader.ReadSingle(),
                            fileReader.ReadSingle(),
                            fileReader.ReadSingle()
                        };
                        break;
                    default:
                        fileReader.BaseStream.Seek(fieldSize, SeekOrigin.Current);
                        break;
                }
            }

            return lscr;
        }
    }
}