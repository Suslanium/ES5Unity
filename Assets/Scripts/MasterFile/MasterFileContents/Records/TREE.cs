using System.IO;

namespace MasterFile.MasterFileContents.Records
{
    /// <summary>
    /// TREE records contain information on trees as well other flora that can be activated.
    /// </summary>
    public class TREE : Record
    {
        /// <summary>
        /// Editor id
        /// </summary>
        public string EditorID { get; private set; }

        /// <summary>
        /// World model filename(path).
        /// </summary>
        public string NifModelFilename { get; private set; }

        private TREE(string type, uint dataSize, uint flag, uint formID, ushort timestamp, ushort versionControlInfo,
            ushort internalRecordVersion, ushort unknownData) : base(type, dataSize, flag, formID, timestamp,
            versionControlInfo, internalRecordVersion, unknownData)
        {
        }

        public static TREE ParseSpecific(Record baseInfo, BinaryReader fileReader, long position)
        {
            var tree = new TREE(baseInfo.Type, baseInfo.DataSize, baseInfo.Flag, baseInfo.FormID, baseInfo.Timestamp,
                baseInfo.VersionControlInfo, baseInfo.InternalRecordVersion, baseInfo.UnknownData);
            while (fileReader.BaseStream.Position < position + baseInfo.DataSize)
            {
                var fieldType = new string(fileReader.ReadChars(4));
                var fieldSize = fileReader.ReadUInt16();
                switch (fieldType)
                {
                    case "EDID":
                        tree.EditorID = new string(fileReader.ReadChars(fieldSize));
                        break;
                    case "MODL":
                        tree.NifModelFilename = new string(fileReader.ReadChars(fieldSize));
                        break;
                    default:
                        fileReader.BaseStream.Seek(fieldSize, SeekOrigin.Current);
                        break;
                }
            }

            return tree;
        }
    }
}