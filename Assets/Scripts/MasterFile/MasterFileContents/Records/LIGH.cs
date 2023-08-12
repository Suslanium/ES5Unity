using System.IO;

namespace MasterFile.MasterFileContents.Records
{
    /// <summary>
    /// LIGH records represent base lighting objects.
    /// </summary>
    public class LIGH : Record
    {
        public string EditorID { get; private set; }

        public string NifModelFilename { get; private set; }

        public string InventoryIconFilename { get; private set; }

        public string MessageIconFilename { get; private set; }

        public int Time { get; private set; }

        public uint Radius { get; private set; }

        public byte[] ColorRGBA { get; private set; }

        public uint Flags { get; private set; }

        public float FalloffExponent { get; private set; }

        public float Fov { get; private set; }

        public float NearClip { get; private set; }

        public float InversePeriod { get; private set; }

        public float IntensityAmplitude { get; private set; }

        public float MovementAmplitude { get; private set; }

        public uint Value { get; private set; }

        public float Weight { get; private set; }

        public float Fade { get; private set; }
        
        public uint HoldingSoundFormID { get; private set; }

        private LIGH(string type, uint dataSize, uint flag, uint formID, ushort timestamp, ushort versionControlInfo,
            ushort internalRecordVersion, ushort unknownData) : base(type, dataSize, flag, formID, timestamp,
            versionControlInfo, internalRecordVersion, unknownData)
        {
        }

        public static LIGH ParseSpecific(Record baseInfo, BinaryReader fileReader, long position)
        {
            var ligh = new LIGH(baseInfo.Type, baseInfo.DataSize, baseInfo.Flag, baseInfo.FormID, baseInfo.Timestamp,
                baseInfo.VersionControlInfo, baseInfo.InternalRecordVersion, baseInfo.UnknownData);
            while (fileReader.BaseStream.Position < position + baseInfo.DataSize)
            {
                var fieldType = new string(fileReader.ReadChars(4));
                var fieldSize = fileReader.ReadUInt16();
                switch (fieldType)
                {
                    case "EDID":
                        ligh.EditorID = new string(fileReader.ReadChars(fieldSize));
                        break;
                    case "MODL":
                        ligh.NifModelFilename = new string(fileReader.ReadChars(fieldSize));
                        break;
                    case "ICON":
                        ligh.InventoryIconFilename = new string(fileReader.ReadChars(fieldSize));
                        break;
                    case "MICO":
                        ligh.MessageIconFilename = new string(fileReader.ReadChars(fieldSize));
                        break;
                    case "DATA":
                        ligh.Time = fileReader.ReadInt32();
                        ligh.Radius = fileReader.ReadUInt32();
                        ligh.ColorRGBA = fileReader.ReadBytes(4);
                        ligh.Flags = fileReader.ReadUInt32();
                        ligh.FalloffExponent = fileReader.ReadSingle();
                        ligh.Fov = fileReader.ReadSingle();
                        ligh.NearClip = fileReader.ReadSingle();
                        ligh.InversePeriod = fileReader.ReadSingle();
                        ligh.IntensityAmplitude = fileReader.ReadSingle();
                        ligh.MovementAmplitude = fileReader.ReadSingle();
                        ligh.Value = fileReader.ReadUInt32();
                        ligh.Weight = fileReader.ReadSingle();
                        break;
                    case "FNAM":
                        ligh.Fade = fileReader.ReadSingle();
                        break;
                    case "SNAM":
                        ligh.HoldingSoundFormID = fileReader.ReadUInt32();
                        break;
                    default:
                        fileReader.BaseStream.Seek(fieldSize, SeekOrigin.Current);
                        break;
                }
            }

            return ligh;
        }
    }
}