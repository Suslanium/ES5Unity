using System;
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
        public float Version { get; protected set; }

        /// <summary>
        /// Number of records and groups (not including TES4 record itself).
        /// </summary>
        public UInt32 EntryAmount { get; protected set; }

        [CanBeNull] public string Author { get; protected set; }

        [CanBeNull] public string Description { get; protected set; }

        /// <summary>
        /// A list of required master files for this 
        /// </summary>
        public string[] MasterFiles { get; protected set; }

        /// <summary>
        /// <para>Overridden forms</para>
        /// <para>This record only appears in ESM flagged files which override their masters' cell children.</para>
        /// <para>An ONAM subrecord will list, exclusively, FormIDs of overridden cell children (ACHR, LAND, NAVM, PGRE, PHZD, REFR).</para>
        /// <para>Number of records is based solely on field size.</para>
        /// </summary>
        [CanBeNull]
        public UInt32[] OverridenForms { get; protected set; }

        /// <summary>
        /// Number of strings that can be tagified (used only for TagifyMasterfile command-line option of the CK).
        /// </summary>
        public UInt32 NumberOfTagifiableStrings { get; protected set; }

        /// <summary>
        /// Some kind of counter. Appears to be related to masters.
        /// </summary>
        public UInt32 Incc { get; protected set; }

        public TES4(string type, uint dataSize, uint flag, uint formID, ushort timestamp, ushort versionControlInfo,
            ushort internalRecordVersion, ushort unknownData, float version, uint entryAmount,
            [CanBeNull] string author, [CanBeNull] string description, string[] masterFiles,
            [CanBeNull] uint[] overridenForms, uint numberOfTagifiableStrings, uint incc) : base(type, dataSize, flag,
            formID, timestamp, versionControlInfo, internalRecordVersion, unknownData)
        {
            Version = version;
            EntryAmount = entryAmount;
            Author = author;
            Description = description;
            MasterFiles = masterFiles;
            OverridenForms = overridenForms;
            NumberOfTagifiableStrings = numberOfTagifiableStrings;
            Incc = incc;
        }

        public TES4 ParseSpecific(BinaryReader fileReader, ulong position)
        {
            throw new NotImplementedException();
        }

        public override Record Parse(BinaryReader fileReader, ulong position)
        {
            return ParseSpecific(fileReader, position);
        }
    }
}