using System.Collections.Generic;
using System.IO;

namespace MasterFile.MasterFileContents
{
    /// <summary>
    /// GRUPs are collections of records, and are used to improve scanning of files to make it easier to skip over records that the reading program is not interested in. In addition to this, subgroups for WRLD and CELLS provide some useful structural information (e.g., the division of cell data into persistent and non-persistent references).
    /// </summary>
    public class Group : MasterFileEntry
    {
        /// <summary>
        /// Size of the entire group, including the group header (24 bytes).
        /// </summary>
        public uint Size { get; protected set; }

        /// <summary>
        /// Label. Format depends on group type (see next field). Stored as 4 bytes.
        /// </summary>
        public byte[] Label { get; protected set; }

        /// <summary>
        /// <para> Group type</para>
        /// <para> Type -> Info -> Label type -> Label description</para>
        /// <para> 0 -> Top level group with records of one type -> char[4] -> Record type</para>
        /// <para> 1 -> World children -> FormID -> Parent world(WRLD)</para>
        /// <para> 2 -> Interior cell block -> Int32 -> Block number</para>
        /// <para> 3 -> Interior cell sub-block -> Int32 -> Sub-block number</para>
        /// <para> 4 -> Exterior cell block -> Int16[2] -> Grid position [Y,X] </para>
        /// <para> 5 -> Exterior cell sub-block -> Int16[2] -> Grid position [Y,X]</para>
        /// <para> 6 -> Cell children -> FormID -> Parent cell (CELL)</para>
        /// <para> 7 -> Topic children -> FormID -> Parent dialog (DIAL)</para>
        /// <para> 8 -> Cell persistent children -> FormID -> Parent cell (CELL) </para>
        /// <para> 9 -> Cell temporary children -> FormID -> Parent cell (CELL)</para>
        /// </summary>
        public int GroupType { get; protected set; }

        /// <summary>
        /// <para>Timestamp</para>
        /// <para>Skyrim: The low byte is the day of the month and the high byte is a combined value representing the month number and last digit of the year times 12. That value is offset, however, so the range is nominally 13-132, representing dates from January 20x4 through December 20x3. Lower values can be seen in Skyrim.esm, likely corresponding to older records held over from Oblivion where values of 1-12 represented 2003 (see the Oblivion version of this page for specifics).</para>
        /// <para>To derive the correct values, use the following formulae, where Y is the single-digit year, M is the month number, and HB is the high byte of the value:</para>
        /// <para>Y = ((HB - 1) / 12 + 3) MOD 10</para>
        /// <para>M = ((HB - 1) MOD 12) + 1</para>
        /// <para>HB = (((Y - 4) MOD 10) + 1) * 12 + M</para>
        /// <para>Skyrim SE: Bits are used to represent each part, with a two-digit year: 0bYYYYYYYMMMMDDDDD. Thus, January 25, 2021 would be (spaces added for clarity): 0b 0010101 0001 11001 or 0x2A39.</para>
        /// </summary>
        public ushort Timestamp { get; protected set; }

        /// <summary>
        /// <para>Version Control Info</para>
        /// <para>The low byte is the user id that last had the form checked out.</para>
        /// <para>The high byte is the user id (if any) that currently has the form checked out.</para>
        /// </summary>
        public ushort VersionControlInfo { get; protected set; }

        /// <summary>
        /// Unknown. The values stored here are significantly different than those used in records and appear to be a 32-bit value rather than two 16-bit values.
        /// </summary>
        public uint UnknownData { get; protected set; }

        /// <summary>
        /// Records and subgroups.
        /// </summary>
        public List<MasterFileEntry> GroupData { get; protected set; } = new();

        private Group(uint size, byte[] label, int groupType, ushort timestamp, ushort versionControlInfo,
            uint unknownData)
        {
            Size = size;
            Label = label;
            GroupType = groupType;
            Timestamp = timestamp;
            VersionControlInfo = versionControlInfo;
            UnknownData = unknownData;
        }

        public new static Group Parse(BinaryReader fileReader, long position)
        {
            Group group = new Group(fileReader.ReadUInt32(), fileReader.ReadBytes(4), fileReader.ReadInt32(),
                fileReader.ReadUInt16(), fileReader.ReadUInt16(), fileReader.ReadUInt32());
            long basePosition = fileReader.BaseStream.Position;
            while (fileReader.BaseStream.Position < basePosition + group.Size - 24)
            {
                group.GroupData.Add(MasterFileEntry.Parse(fileReader, fileReader.BaseStream.Position));
            }

            return group;
        }
    }
}