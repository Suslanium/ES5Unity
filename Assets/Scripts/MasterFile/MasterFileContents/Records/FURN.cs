using System.IO;

namespace MasterFile.MasterFileContents.Records
{
    /// <summary>
    /// FURN records contain information on furniture.
    /// </summary>
    public class FURN: Record
    {
        /// <summary>
        /// Editor id
        /// </summary>
        public string EditorID { get; private set; }
        
        /// <summary>
        /// Full (in-game) id (localized string id)
        /// </summary>
        public uint InGameNameID { get; private set; }
        
        /// <summary>
        /// World model filename(path)
        /// </summary>
        public string NifModelFilename { get; private set; }
        
        /// <summary>
        /// <para>0 : None</para>
        /// <para>1 : Create Object</para>
        /// <para>2 : Smithing Weapon</para>
        /// <para>3 : Enchanting</para>
        /// <para>4 : Enchanting Experiment</para>
        /// <para>5 : Alchemy</para>
        /// <para>6 : Alchemy Experiment</para>
        /// <para>7 : Smithing Armor</para>
        /// </summary>
        public byte WorkbenchType { get; private set; }
        
        /// <summary>
        /// ActorValue Skill for using the workbench (one of 18 AV or 0xFF for none)
        /// </summary>
        public byte WorkbenchSkill { get; private set; }
        
        /// <summary>
        /// KYWD FormID
        /// </summary>
        public uint InteractionKeyword { get; private set; }

        private FURN(string type, uint dataSize, uint flag, uint formID, ushort timestamp, ushort versionControlInfo, ushort internalRecordVersion, ushort unknownData) : base(type, dataSize, flag, formID, timestamp, versionControlInfo, internalRecordVersion, unknownData)
        {
        }

        public static FURN ParseSpecific(Record baseInfo, BinaryReader fileReader, long position)
        {
            var furn = new FURN(baseInfo.Type, baseInfo.DataSize, baseInfo.Flag, baseInfo.FormID, baseInfo.Timestamp,
                baseInfo.VersionControlInfo, baseInfo.InternalRecordVersion, baseInfo.UnknownData);
            while (fileReader.BaseStream.Position < position + baseInfo.DataSize)
            {
                var fieldType = new string(fileReader.ReadChars(4));
                var fieldSize = fileReader.ReadUInt16();
                switch (fieldType)
                {
                    case "EDID":
                        furn.EditorID = new string(fileReader.ReadChars(fieldSize));
                        break;
                    case "FULL":
                        furn.InGameNameID = fileReader.ReadUInt32();
                        break;
                    case "WBDT":
                        furn.WorkbenchType = fileReader.ReadByte();
                        furn.WorkbenchSkill = fileReader.ReadByte();
                        break;
                    case "KNAM":
                        furn.InteractionKeyword = fileReader.ReadUInt32();
                        break;
                    case "MODL":
                        furn.NifModelFilename = "Meshes/" + new string(fileReader.ReadChars(fieldSize)).Replace("\0", string.Empty);
                        break;
                    default:
                        fileReader.BaseStream.Seek(fieldSize, SeekOrigin.Current);
                        break;
                }
            }

            return furn;
        }
    }
}