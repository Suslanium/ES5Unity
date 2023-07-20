using System.IO;

namespace MasterFile.MasterFileContents.Records
{
    /// <summary>
    /// STAT records contain information on static objects.
    /// </summary>
    public class STAT : Record
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
        /// MaxAngle 30-120 degrees
        /// </summary>
        public float DirMaterialMaxAngle { get; private set; }

        /// <summary>
        /// Directional Material - MATO formID
        /// </summary>
        public uint DirectionalMaterialFormID { get; private set; }

        private STAT(string type, uint dataSize, uint flag, uint formID, ushort timestamp, ushort versionControlInfo, ushort internalRecordVersion, ushort unknownData) : base(type, dataSize, flag, formID, timestamp, versionControlInfo, internalRecordVersion, unknownData)
        {
        }

        public static STAT ParseSpecific(Record baseInfo, BinaryReader fileReader, long position)
        {
            var stat = new STAT(baseInfo.Type, baseInfo.DataSize, baseInfo.Flag, baseInfo.FormID, baseInfo.Timestamp,
                baseInfo.VersionControlInfo, baseInfo.InternalRecordVersion, baseInfo.UnknownData);
            while (fileReader.BaseStream.Position < position + baseInfo.DataSize)
            {
                var fieldType = new string(fileReader.ReadChars(4));
                var fieldSize = fileReader.ReadUInt16();
                switch (fieldType)
                {
                    case "EDID":
                        stat.EditorID = new string(fileReader.ReadChars(fieldSize));
                        break;
                    case "MODL":
                        stat.NifModelFilename = new string(fileReader.ReadChars(fieldSize));
                        break;
                    case "DNAM":
                        stat.DirMaterialMaxAngle = fileReader.ReadSingle();
                        stat.DirectionalMaterialFormID = fileReader.ReadUInt32();
                        break;
                    default:
                        fileReader.BaseStream.Seek(fieldSize, SeekOrigin.Current);
                        break;
                }
            }

            return stat;
        }
    }
}