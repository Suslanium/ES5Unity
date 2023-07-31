using System.IO;

namespace MasterFile.MasterFileContents.Records
{
    /// <summary>
    /// MSTT records contain information on movable static objects.
    /// </summary>
    public class MSTT: Record
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
        /// Ambient Sound (SNDR)
        /// </summary>
        public uint AmbientLoopingSoundReference { get; private set; }

        private MSTT(string type, uint dataSize, uint flag, uint formID, ushort timestamp, ushort versionControlInfo, ushort internalRecordVersion, ushort unknownData) : base(type, dataSize, flag, formID, timestamp, versionControlInfo, internalRecordVersion, unknownData)
        {
        }

        public static MSTT ParseSpecific(Record baseInfo, BinaryReader fileReader, long position)
        {
            var mstt = new MSTT(baseInfo.Type, baseInfo.DataSize, baseInfo.Flag, baseInfo.FormID, baseInfo.Timestamp,
                baseInfo.VersionControlInfo, baseInfo.InternalRecordVersion, baseInfo.UnknownData);
            while (fileReader.BaseStream.Position < position + baseInfo.DataSize)
            {
                var fieldType = new string(fileReader.ReadChars(4));
                var fieldSize = fileReader.ReadUInt16();
                switch (fieldType)
                {
                    case "EDID":
                        mstt.EditorID = new string(fileReader.ReadChars(fieldSize));
                        break;
                    case "MODL":
                        mstt.NifModelFilename = "Meshes/" + new string(fileReader.ReadChars(fieldSize)).Replace("\0", string.Empty);
                        break;
                    case "SNAM":
                        mstt.AmbientLoopingSoundReference = fileReader.ReadUInt32();
                        break;
                    default:
                        fileReader.BaseStream.Seek(fieldSize, SeekOrigin.Current);
                        break;
                }
            }

            return mstt;
        }
    }
}