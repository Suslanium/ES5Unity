using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;

namespace MasterFile.MasterFileContents.Records
{
    /// <summary>
    /// TES4 is the header record for the mod file. It contains info like author, description, file type, and masters list.
    /// </summary>
    public class TES4 : Record
    {
        /// <summary>
        /// File version (0.94 for older files; 1.7 for more recent ones).
        /// </summary>
        public float Version { get; private set; }

        /// <summary>
        /// Number of records and groups (not including TES4 record itself).
        /// </summary>
        public uint EntryAmount { get; private set; }

        [CanBeNull] public string Author { get; private set; }

        [CanBeNull] public string Description { get; private set; }

        /// <summary>
        /// A list of required master files for this 
        /// </summary>
        public List<string> MasterFiles { get; private set; } = new();

        /// <summary>
        /// <para>Overridden forms</para>
        /// <para>This record only appears in ESM flagged files which override their masters' cell children.</para>
        /// <para>An ONAM subrecord will list, exclusively, FormIDs of overridden cell children (ACHR, LAND, NAVM, PGRE, PHZD, REFR).</para>
        /// <para>Number of records is based solely on field size.</para>
        /// </summary>
        public List<uint> OverridenForms { get; private set; } = new();

        /// <summary>
        /// Number of strings that can be tagified (used only for TagifyMasterfile command-line option of the CK).
        /// </summary>
        public uint NumberOfTagifiableStrings { get; private set; }

        /// <summary>
        /// Some kind of counter. Appears to be related to masters.
        /// </summary>
        public uint Incc { get; private set; }

        private TES4(string type, uint dataSize, uint flag, uint formID, ushort timestamp, ushort versionControlInfo,
            ushort internalRecordVersion, ushort unknownData) : base(type, dataSize, flag, formID, timestamp,
            versionControlInfo, internalRecordVersion, unknownData)
        {
        }

        public static TES4 ParseSpecific(Record baseInfo, BinaryReader fileReader, long position)
        {
            var header = new TES4(baseInfo.Type, baseInfo.DataSize, baseInfo.Flag, baseInfo.FormID, baseInfo.Timestamp,
                baseInfo.VersionControlInfo, baseInfo.InternalRecordVersion, baseInfo.UnknownData);
            while (fileReader.BaseStream.Position < position + baseInfo.DataSize)
            {
                var fieldType = new string(fileReader.ReadChars(4));
                var fieldSize = fileReader.ReadUInt16();
                switch (fieldType)
                {
                    case "HEDR":
                        header.Version = fileReader.ReadSingle();
                        header.EntryAmount = fileReader.ReadUInt32();
                        fileReader.ReadUInt32();
                        break;
                    case "CNAM":
                        header.Author = new string(fileReader.ReadChars(fieldSize));
                        break;
                    case "SNAM":
                        header.Description = new string(fileReader.ReadChars(fieldSize));
                        break;
                    case "MAST":
                        header.MasterFiles.Add(new string(fileReader.ReadChars(fieldSize)));
                        break;
                    case "DATA":
                        fileReader.ReadChars(fieldSize);
                        break;
                    case "ONAM":
                        for (var i = 0; i < fieldSize / 4; i++)
                        {
                            header.OverridenForms.Add(fileReader.ReadUInt32());
                        }
                        break;
                    case "INTV":
                        header.NumberOfTagifiableStrings = fileReader.ReadUInt32();
                        break;
                    case "INCC":
                        header.Incc = fileReader.ReadUInt32();
                        break;
                    default:
                        fileReader.BaseStream.Seek(fieldSize, SeekOrigin.Current);
                        break;
                }
            }

            return header;
        }
    }
}