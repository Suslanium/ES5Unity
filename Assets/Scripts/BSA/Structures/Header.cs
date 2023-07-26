using System.Collections.Generic;
using System.IO;
using BSA.Structures.Enums;

namespace BSA.Structures
{
    public class Header
    {
        public uint Version { get; private set; }
        public List<ArchiveFlag> ArchiveFlags { get; private set; } = new();
        public uint FolderCount { get; private set; }
        public uint FileCount { get; private set; }
        public uint TotalFolderNameLength { get; private set; }
        public uint TotalFileNameLength { get; private set; }
        public List<FileTypeFlag> FileFlags { get; private set; } = new();

        private Header()
        {
        }

        public static Header Parse(BinaryReader binaryReader)
        {
            var header = new Header();
            binaryReader.BaseStream.Seek(4, SeekOrigin.Current);
            header.Version = binaryReader.ReadUInt32();
            binaryReader.BaseStream.Seek(4, SeekOrigin.Current);
            var archiveFlags = binaryReader.ReadUInt32();
            if ((archiveFlags & 0x1) != 0)
            {
                header.ArchiveFlags.Add(ArchiveFlag.IncludeDirNames);
            }

            if ((archiveFlags & 0x2) != 0)
            {
                header.ArchiveFlags.Add(ArchiveFlag.IncludeFileNames);
            }

            if ((archiveFlags & 0x4) != 0)
            {
                header.ArchiveFlags.Add(ArchiveFlag.CompressedArchive);
            }

            if ((archiveFlags & 0x8) != 0)
            {
                header.ArchiveFlags.Add(ArchiveFlag.RetainDirNames);
            }

            if ((archiveFlags & 0x10) != 0)
            {
                header.ArchiveFlags.Add(ArchiveFlag.RetainFileNames);
            }

            if ((archiveFlags & 0x20) != 0)
            {
                header.ArchiveFlags.Add(ArchiveFlag.RetainFileOffsets);
            }

            if ((archiveFlags & 0x40) != 0)
            {
                header.ArchiveFlags.Add(ArchiveFlag.Xbox360Archive);
            }

            if ((archiveFlags & 0x80) != 0)
            {
                header.ArchiveFlags.Add(ArchiveFlag.RetainStringsDuringStartup);
            }

            if ((archiveFlags & 0x100) != 0)
            {
                header.ArchiveFlags.Add(ArchiveFlag.EmbedFileNames);
            }

            if ((archiveFlags & 0x200) != 0)
            {
                header.ArchiveFlags.Add(ArchiveFlag.UsesXMemCodec);
            }

            header.FolderCount = binaryReader.ReadUInt32();
            header.FileCount = binaryReader.ReadUInt32();
            header.TotalFolderNameLength = binaryReader.ReadUInt32();
            header.TotalFileNameLength = binaryReader.ReadUInt32();
            var fileFlags = binaryReader.ReadUInt16();
            if ((fileFlags & 0x1) != 0)
            {
                header.FileFlags.Add(FileTypeFlag.Meshes);
            }

            if ((fileFlags & 0x2) != 0)
            {
                header.FileFlags.Add(FileTypeFlag.Textures);
            }

            if ((fileFlags & 0x4) != 0)
            {
                header.FileFlags.Add(FileTypeFlag.Menus);
            }

            if ((fileFlags & 0x8) != 0)
            {
                header.FileFlags.Add(FileTypeFlag.Sounds);
            }

            if ((fileFlags & 0x10) != 0)
            {
                header.FileFlags.Add(FileTypeFlag.Voices);
            }

            if ((fileFlags & 0x20) != 0)
            {
                header.FileFlags.Add(FileTypeFlag.Shaders);
            }

            if ((fileFlags & 0x40) != 0)
            {
                header.FileFlags.Add(FileTypeFlag.Trees);
            }

            if ((fileFlags & 0x80) != 0)
            {
                header.FileFlags.Add(FileTypeFlag.Fonts);
            }

            if ((fileFlags & 0x100) != 0)
            {
                header.FileFlags.Add(FileTypeFlag.Miscellaneous);
            }

            binaryReader.BaseStream.Seek(2, SeekOrigin.Current);

            return header;
        }
    }
}