using System.IO;
using JetBrains.Annotations;

namespace MasterFile.MasterFileContents.Records
{
    public class TXST : Record
    {
        public string EditorID { get; private set; }

        public string DiffuseMapPath { get; private set; }

        [CanBeNull] public string NormalMapPath { get; private set; }

        [CanBeNull] public string MaskMapPath { get; private set; }

        [CanBeNull] public string GlowMapPath { get; private set; }

        [CanBeNull] public string DetailMapPath { get; private set; }

        [CanBeNull] public string EnvironmentMapPath { get; private set; }

        [CanBeNull] public string MultiLayerMapPath { get; private set; }

        [CanBeNull] public string SpecularMapPath { get; private set; }

        /// <summary>
        /// flags:
        /// 0x01 - not Has specular map;
        /// 0x02 - Facegen Textures;
        /// 0x04 - Has model space normal map;
        /// </summary>
        public ushort Flags { get; private set; }

        private TXST(string type, uint dataSize, uint flag, uint formID, ushort timestamp, ushort versionControlInfo,
            ushort internalRecordVersion, ushort unknownData) : base(type, dataSize, flag, formID, timestamp,
            versionControlInfo, internalRecordVersion, unknownData)
        {
        }

        public static TXST ParseSpecific(Record baseInfo, BinaryReader fileReader, long position)
        {
            var txst = new TXST(baseInfo.Type, baseInfo.DataSize, baseInfo.Flag, baseInfo.FormID, baseInfo.Timestamp,
                baseInfo.VersionControlInfo, baseInfo.InternalRecordVersion, baseInfo.UnknownData);
            while (fileReader.BaseStream.Position < position + baseInfo.DataSize)
            {
                var fieldType = new string(fileReader.ReadChars(4));
                var fieldSize = fileReader.ReadUInt16();
                switch (fieldType)
                {
                    case "EDID":
                        txst.EditorID = new string(fileReader.ReadChars(fieldSize));
                        break;
                    case "TX00":
                        txst.DiffuseMapPath = new string(fileReader.ReadChars(fieldSize));
                        break;
                    case "TX01":
                        txst.NormalMapPath = new string(fileReader.ReadChars(fieldSize));
                        break;
                    case "TX02":
                        txst.MaskMapPath = new string(fileReader.ReadChars(fieldSize));
                        break;
                    case "TX03":
                        txst.GlowMapPath = new string(fileReader.ReadChars(fieldSize));
                        break;
                    case "TX04":
                        txst.DetailMapPath = new string(fileReader.ReadChars(fieldSize));
                        break;
                    case "TX05":
                        txst.EnvironmentMapPath = new string(fileReader.ReadChars(fieldSize));
                        break;
                    case "TX06":
                        txst.MultiLayerMapPath = new string(fileReader.ReadChars(fieldSize));
                        break;
                    case "TX07":
                        txst.SpecularMapPath = new string(fileReader.ReadChars(fieldSize));
                        break;
                    case "DNAM":
                        txst.Flags = fileReader.ReadUInt16();
                        break;
                    default:
                        fileReader.BaseStream.Seek(fieldSize, SeekOrigin.Current);
                        break;
                }
            }

            return txst;
        }
    }
}