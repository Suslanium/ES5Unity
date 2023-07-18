using System.IO;
using UnityEngine;

namespace MasterFile.MasterFileContents.Records
{
    /// <summary>
    /// <para>WRLD records contain the data for a 'world', which can be all of Tamriel or a handful of cells.</para>
    /// In the WRLD GRUP, following each WRLD record is a nested GRUP which contains a single CELL record (presumably the starting location for the world) and a number of Exterior Cell Block groups (see the group table). Each Exterior Cell Block group contains Exterior Cell Sub-Block groups, each of which contain CELL records.
    /// </summary>
    public class WRLD : Record
    {
        /// <summary>
        /// The name of this worldspace used in the construction kit
        /// </summary>
        public string EditorID { get; private set; }

        /// <summary>
        /// The name of this worldspace used in the game(lstring)
        /// </summary>
        public uint LocalizedNameStringID { get; private set; }

        /// <summary>
        /// X, Y
        /// </summary>
        public short[] CenterCellCoordinates { get; private set; }

        /// <summary>
        /// Interior Lighting LGTM
        /// </summary>
        public uint InteriorLightingReference { get; private set; }

        /// <summary>
        /// <para>Flags</para>
        /// <para>0x01 - Small World</para>
        /// <para>0x02 - Can't Fast Travel From Here</para>
        /// <para>0x04</para>
        /// <para>0x08 - No LOD Water</para>
        /// <para>0x10 - No Landscape</para>
        /// <para>0x20 - No Sky</para>
        /// <para>0x40 - Fixed Dimensions</para>
        /// <para>0x80 - No Grass</para>
        /// </summary>
        public byte WorldFlag { get; private set; }

        /// <summary>
        /// Form ID of the parent worldspace.
        /// </summary>
        public uint ParentWorldReference { get; private set; }
        
        /// <summary>
        /// <para>Use flags - Set if parts are inherited from parent worldspace WNAM</para>
        /// <para>0x01 - Use Land Data (DNAM)</para>
        /// <para>0x02 - Use LOD Data (NAM3, NAM4)</para>
        /// <para>0x04 - Use Map Data (MNAM, MODL)</para>
        /// <para>0x08 - Use Water Data (NAM2)</para>
        /// <para>0x10 - unknown</para>
        /// <para>0x20 - Use Climate Data (CNAM)</para>
        /// <para>0x40 - Use Sky Cell</para>
        /// </summary>
        public ushort ParentWorldRelatedFlags { get; private set; }

        /// <summary>
        /// Location LCTN
        /// </summary>
        public uint ExitLocationReference { get; private set; }

        /// <summary>
        /// CLMT reference
        /// </summary>
        public uint ClimateReference { get; private set; }

        /// <summary>
        /// Default land- and oceanwater-levels (-27000 & -14000.0 for Tamriel)
        /// </summary>
        public float[] LandData { get; private set; }

        private WRLD(string type, uint dataSize, uint flag, uint formID, ushort timestamp, ushort versionControlInfo,
            ushort internalRecordVersion, ushort unknownData) : base(type, dataSize, flag, formID, timestamp,
            versionControlInfo, internalRecordVersion, unknownData)
        {
        }

        public static WRLD ParseSpecific(Record baseInfo, BinaryReader fileReader, long position)
        {
            WRLD wrld = new WRLD(baseInfo.Type, baseInfo.DataSize, baseInfo.Flag, baseInfo.FormID, baseInfo.Timestamp,
                baseInfo.VersionControlInfo, baseInfo.InternalRecordVersion, baseInfo.UnknownData);
            while (fileReader.BaseStream.Position < position + baseInfo.DataSize)
            {
                string fieldType = new string(fileReader.ReadChars(4));
                ushort fieldSize = fileReader.ReadUInt16();
                switch (fieldType)
                {
                    case "EDID":
                        wrld.EditorID = new string(fileReader.ReadChars(fieldSize));
                        break;
                    case "FULL":
                        wrld.LocalizedNameStringID = fileReader.ReadUInt32();
                        break;
                    case "WCTR":
                        short x = fileReader.ReadInt16();
                        short y = fileReader.ReadInt16();
                        wrld.CenterCellCoordinates = new[] { x, y };
                        break;
                    case "LTMP":
                        wrld.InteriorLightingReference = fileReader.ReadUInt32();
                        break;
                    case "DATA":
                        wrld.WorldFlag = fileReader.ReadByte();
                        break;
                    case "WNAM":
                        wrld.ParentWorldReference = fileReader.ReadUInt32();
                        break;
                    case "XLCN":
                        wrld.ExitLocationReference = fileReader.ReadUInt32();
                        break;
                    case "CNAM":
                        wrld.ClimateReference = fileReader.ReadUInt32();
                        break;
                    case "DNAM":
                        float land = fileReader.ReadSingle();
                        float ocean = fileReader.ReadSingle();
                        wrld.LandData = new[] { land, ocean };
                        break;
                    case "PNAM":
                        wrld.ParentWorldRelatedFlags = fileReader.ReadUInt16();
                        break;
                    case "OFST":
                        fileReader.BaseStream.Seek(position + baseInfo.DataSize, SeekOrigin.Begin);
                        return wrld;
                    default:
                        fileReader.BaseStream.Seek(fieldSize, SeekOrigin.Current);
                        break;
                }
            }
            
            return wrld;
        }
    }
}