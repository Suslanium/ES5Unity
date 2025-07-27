using System.IO;

namespace MasterFile.MasterFileContents.Records
{
    /// <summary>
    /// Land texture record
    /// </summary>
    public class LTEX : Record
    {
        public string EditorID { get; private set; }
        
        public uint? TextureFormID { get; private set; }
        
        private LTEX(string type, uint dataSize, uint flag, uint formID, ushort timestamp, ushort versionControlInfo,
            ushort internalRecordVersion, ushort unknownData) : base(type, dataSize, flag, formID, timestamp,
            versionControlInfo, internalRecordVersion, unknownData)
        {
        }

        public static LTEX ParseSpecific(Record baseInfo, BinaryReader fileReader, long position)
        {
            var ltex = new LTEX(baseInfo.Type, baseInfo.DataSize, baseInfo.Flag, baseInfo.FormID, baseInfo.Timestamp,
                baseInfo.VersionControlInfo, baseInfo.InternalRecordVersion, baseInfo.UnknownData);
            while (fileReader.BaseStream.Position < position + baseInfo.DataSize)
            {
                var fieldType = new string(fileReader.ReadChars(4));
                var fieldSize = fileReader.ReadUInt16();
                switch (fieldType)
                {
                    case "EDID":
                        ltex.EditorID = new string(fileReader.ReadChars(fieldSize));
                        break;
                    case "TNAM":
                        ltex.TextureFormID = fileReader.ReadUInt32();
                        break;
                    default:
                        fileReader.BaseStream.Seek(fieldSize, SeekOrigin.Current);
                        break;
                }
            }

            return ltex;
        }
    }
}