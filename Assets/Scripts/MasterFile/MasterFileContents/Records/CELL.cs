using System.IO;
using MasterFile.MasterFileContents.Records.Structures;

namespace MasterFile.MasterFileContents.Records
{
    /// <summary>
    /// <para>CELL records contain the data for either interior (CELL top group) or exterior (WRLD top group) cells. They follow the same format in both cases. The CELL record is followed by a group containing the references for the cell, organized into subgroups. Persistent references are defined in the 'persistent children' subgroup while temporary references are defined in the 'temporary children' subgroup. The 'persistent children' subgroup must occur before the 'temporary children' subgroup.</para>
    /// <para>The block and sub-block groups for an interior cell can be determined either by the label field of the group headers of the block and sub-block, or by the last two decimal digits of the Form ID. For example, for Form ID 0x138CA, which is 80,074 in decimal, 4 indicates that this is block 4, and the 7 indicates that it's sub-block 7.</para>
    /// <para>The block and sub-block groups for an exterior cell can be determined either by the label filed of the group headers of the block and sub-block, or by the X, Y coordinates of the cell from the XCLC field. Each block contains 16 sub-blocks (4x4) and each sub-block contains 64 cells (8x8). So, to get the sub-block number, divide the coordinates by 8 (rounding down), and the block number can be determined by dividing the sub-block numbers by 4 (again, rounding down). Thus, a cell with the X, Y coordinates 31, 50 would be in floor(31 / 8) = 3, floor(50 / 8) = 6, so sub-block 3, 6. Dividing again, only this time by 4, it would be in block 0, 1.</para>
    /// </summary>
    public class CELL : Record
    {
        public string EditorID { get; private set; }

        public uint LocalizedNameID { get; private set; }

        /// <summary>
        /// <para>flags - Sometimes the field is only one byte long</para>
        /// <para>0x0001 - Interior</para>
        /// <para>0x0002 - Has Water</para>
        /// <para>0x0004 - not Can't Travel From Here - only valid for interior cells</para>
        /// <para>0x0008 - No LOD Water</para>
        /// <para>0x0020 - Public Area</para>
        /// <para>0x0040 - Hand Changed</para>
        /// <para>0x0080 - Show Sky</para>
        /// <para>0x0100 - Use Sky Lighting</para>
        /// </summary>
        public ushort CellFlag { get; private set; }

        /// <summary>
        /// Always in exterior cells and never in interior cells.
        /// </summary>
        public int XGridPosition { get; private set; }

        /// <summary>
        /// Always in exterior cells and never in interior cells.
        /// </summary>
        public int YGridPosition { get; private set; }

        public Lighting CellLightingInfo { get; private set; }

        /// <summary>
        /// The lighting template for this cell.
        /// </summary>
        public uint LightingTemplateReference { get; private set; }

        /// <summary>
        /// <para>Non-ocean water-height in cell, is used for rivers, ponds etc., ocean-water is globally defined elsewhere.</para>
        /// <para>0x7F7FFFFF reserved as ID for "no water present", it is also the maximum positive float.</para>
        /// <para>0x4F7FFFC9 is a bug in the CK, this is the maximum unsigned integer 2^32-1 cast to a float and means the same as above</para>
        /// <para>0xCF000000 could be a bug as well, this is the maximum signed negative integer -2^31 cast to a float</para>
        /// </summary>
        public float NonOceanWaterHeight { get; private set; }

        /// <summary>
        /// The location for (of?) this cell.
        /// </summary>
        public uint LocationReference { get; private set; }

        /// <summary>
        /// The water for (of?) this cell.
        /// </summary>
        public uint WaterReference { get; private set; }

        /// <summary>
        /// Water Environment Map (only interior cells)
        /// </summary>
        public string WaterEnvironmentMap { get; private set; }

        /// <summary>
        /// The acoustic space for this cell.
        /// </summary>
        public uint AcousticSpaceReference { get; private set; }

        /// <summary>
        /// The music type for this cell.
        /// </summary>
        public uint MusicTypeReference { get; private set; }

        /// <summary>
        /// The image space for this cell.
        /// </summary>
        public uint ImageSpaceReference { get; private set; }

        private CELL(string type, uint dataSize, uint flag, uint formID, ushort timestamp, ushort versionControlInfo,
            ushort internalRecordVersion, ushort unknownData) : base(type, dataSize, flag, formID, timestamp,
            versionControlInfo, internalRecordVersion, unknownData)
        {
        }

        public static CELL ParseSpecific(Record baseInfo, BinaryReader fileReader, long position)
        {
            var cell = new CELL(baseInfo.Type, baseInfo.DataSize, baseInfo.Flag, baseInfo.FormID, baseInfo.Timestamp,
                baseInfo.VersionControlInfo, baseInfo.InternalRecordVersion, baseInfo.UnknownData);
            while (fileReader.BaseStream.Position < position + baseInfo.DataSize)
            {
                var fieldType = new string(fileReader.ReadChars(4));
                var fieldSize = fileReader.ReadUInt16();
                switch (fieldType)
                {
                    case "EDID":
                        cell.EditorID = new string(fileReader.ReadChars(fieldSize));
                        break;
                    case "FULL":
                        cell.LocalizedNameID = fileReader.ReadUInt32();
                        break;
                    case "DATA":
                        cell.CellFlag = fieldSize == 1 ? fileReader.ReadByte() : fileReader.ReadUInt16();
                        break;
                    case "XCLC":
                        cell.XGridPosition = fileReader.ReadInt32();
                        cell.YGridPosition = fileReader.ReadInt32();
                        fileReader.ReadUInt32();
                        break;
                    case "XCLL":
                        cell.CellLightingInfo = Lighting.ParseFromCell(fieldSize, fileReader);
                        break;
                    case "LTMP":
                        cell.LightingTemplateReference = fileReader.ReadUInt32();
                        break;
                    case "XCLW":
                        cell.NonOceanWaterHeight = fileReader.ReadSingle();
                        break;
                    case "XLCN":
                        cell.LocationReference = fileReader.ReadUInt32();
                        break;
                    case "XCWT":
                        cell.WaterReference = fileReader.ReadUInt32();
                        break;
                    case "XWEM":
                        cell.WaterEnvironmentMap = new string(fileReader.ReadChars(fieldSize));
                        break;
                    case "XCAS":
                        cell.AcousticSpaceReference = fileReader.ReadUInt32();
                        break;
                    case "XCMO":
                        cell.MusicTypeReference = fileReader.ReadUInt32();
                        break;
                    case "XCIM":
                        cell.ImageSpaceReference = fileReader.ReadUInt32();
                        break;
                    default:
                        fileReader.BaseStream.Seek(fieldSize, SeekOrigin.Current);
                        break;
                }
            }

            return cell;
        }
    }
}