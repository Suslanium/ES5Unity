using System.IO;
using MasterFile.MasterFileContents.Records.Structures;

namespace MasterFile.MasterFileContents.Records
{
    /// <summary>
    /// LGTM records contain information on lighting templates.
    /// </summary>
    public class LGTM : Record
    {
        public string EditorID { get; private set; }

        public Lighting LightingData { get; private set; }

        private LGTM(string type, uint dataSize, uint flag, uint formID, ushort timestamp, ushort versionControlInfo,
            ushort internalRecordVersion, ushort unknownData) : base(type, dataSize, flag, formID, timestamp,
            versionControlInfo, internalRecordVersion, unknownData)
        {
        }

        public static LGTM ParseSpecific(Record baseInfo, BinaryReader fileReader, long position)
        {
            var lgtm = new LGTM(baseInfo.Type, baseInfo.DataSize, baseInfo.Flag, baseInfo.FormID, baseInfo.Timestamp,
                baseInfo.VersionControlInfo, baseInfo.InternalRecordVersion, baseInfo.UnknownData);
            while (fileReader.BaseStream.Position < position + baseInfo.DataSize)
            {
                var fieldType = new string(fileReader.ReadChars(4));
                var fieldSize = fileReader.ReadUInt16();
                switch (fieldType)
                {
                    case "EDID":
                        lgtm.EditorID = new string(fileReader.ReadChars(fieldSize));
                        break;
                    case "DATA":
                        lgtm.LightingData = Lighting.Parse(fieldSize, fileReader);
                        break;
                    default:
                        fileReader.BaseStream.Seek(fieldSize, SeekOrigin.Current);
                        break;
                }
            }

            return lgtm;
        }
    }
}