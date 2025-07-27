using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NIF.Parser.NiObjects
{
    public class Header
    {
        /// <summary>
        /// 'NetImmerse File Format x.x.x.x' (versions &lt;= 10.0.1.2) or 'Gamebryo File Format x.x.x.x' (versions &gt;= 10.1.0.0), with x.x.x.x the version written out. Ends with a newline character (0x0A).
        /// </summary>
        public string HeaderString { get; private set; }

        /// <summary>
        /// A 32-bit integer that stores the version in hexadecimal format with each byte representing a number in the version string.
        /// </summary>
        public uint Version { get; private set; }

        /// <summary>
        /// 0 - The numbers are stored in big endian format, such as those used by PowerPC Mac processors.
        /// 1 - The numbers are stored in little endian format, such as those used by Intel and AMD x86 processors.
        /// </summary>
        public byte EndianType { get; private set; }

        /// <summary>
        /// An extra version number, for companies that decide to modify the file format.
        /// (Stored in little endian format)
        /// </summary>
        public uint UserVersion { get; private set; }

        /// <summary>
        /// Number of file objects.
        /// (Stored in little endian format)
        /// </summary>
        public uint NumberOfBlocks { get; private set; }
        
        public uint BethesdaVersion { get; private set; }
        
        public string Author { get; private set; }
        
        /// <summary>
        /// Number of object types in this NIF file.
        /// </summary>
        public ushort NumberOfBlockTypes { get; private set; }
        
        /// <summary>
        /// List of all object types used in this NIF file.
        /// </summary>
        public string[] BlockTypes { get; private set; }
        
        /// <summary>
        /// Maps file objects on their corresponding type: first file object is of type object_types[object_type_index[0]], the second of object_types[object_type_index[1]], etc.
        /// </summary>
        public ushort[] BlockTypeIndex { get; private set; }
        
        /// <summary>
        /// Array of block sizes
        /// </summary>
        public uint[] BlockSizes { get; private set; }
        
        public uint NumberOfStrings { get; private set; }
        
        public uint MaximumStringLength { get; private set; }
        
        public string[] Strings { get; private set; }

        public uint NumberOfGroups { get; private set; }
        
        public uint[] Groups { get; private set; }

        private Header()
        {}

        /// <summary>
        /// This method is meant to ONLY be used from NIFReader.
        /// </summary>
        public static Header ParseHeader(BinaryReader nifReader)
        {
            var header = new Header();
            var headerStringBytes = new List<byte>();
            do
            {
                var currentByte = nifReader.ReadByte();
                headerStringBytes.Add(currentByte);
            } while (headerStringBytes.Last() != 0x0A);

            header.HeaderString = System.Text.Encoding.UTF8.GetString(headerStringBytes.ToArray());
            header.Version = nifReader.ReadUInt32();
            if (header.Version >= 0x14000003)
            {
                header.EndianType = nifReader.ReadByte();
                if (header.EndianType == 0)
                {
                    throw new NotImplementedException("Big endian nif file reading is not supported");
                }
            }
            if (header.Version >= 0x0A000108) header.UserVersion = nifReader.ReadUInt32();
            header.NumberOfBlocks = nifReader.ReadUInt32();
            if (header.Version == 0x0A000102 || (header.Version == 0x14020007 || header.Version == 0x14020005 || (header.Version is >= 0x0A010000 and <= 0x14000004 && header.UserVersion <= 11)) && header.UserVersion >= 3)
            {
                header.BethesdaVersion = nifReader.ReadUInt32();
                header.Author = NifReaderUtils.ReadExportString(nifReader);

                //Skipping some stuff
                if (Conditions.BsGt130(header)) nifReader.BaseStream.Seek(4, SeekOrigin.Current);
                if (header.BethesdaVersion < 131)
                {
                    NifReaderUtils.ReadExportString(nifReader);
                }

                NifReaderUtils.ReadExportString(nifReader);
                if (header.BethesdaVersion >= 103)
                {
                    NifReaderUtils.ReadExportString(nifReader);
                }
            }

            header.NumberOfBlockTypes = nifReader.ReadUInt16();
            header.BlockTypes = NifReaderUtils.ReadSizedStringArray(nifReader, header.NumberOfBlockTypes);
            header.BlockTypeIndex = NifReaderUtils.ReadUshortArray(nifReader, header.NumberOfBlocks);
            if (header.Version >= 0x14020005) header.BlockSizes = NifReaderUtils.ReadUintArray(nifReader, header.NumberOfBlocks);
            if (header.Version >= 0x14010001) header.NumberOfStrings = nifReader.ReadUInt32();
            if (header.Version >= 0x14010001) header.MaximumStringLength = nifReader.ReadUInt32();
            if (header.Version >= 0x14010001) header.Strings = NifReaderUtils.ReadSizedStringArray(nifReader, header.NumberOfStrings);
            header.NumberOfGroups = nifReader.ReadUInt32();
            header.Groups = NifReaderUtils.ReadUintArray(nifReader, header.NumberOfGroups);
            return header;
        }
    }
}