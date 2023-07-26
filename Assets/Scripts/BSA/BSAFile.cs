using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BSA.Structures;
using BSA.Structures.Enums;
using Ionic.Zlib;
using UnityEngine;

namespace BSA
{
    /// <summary>
    /// BSA files are the resource archive files used by Skyrim.
    /// </summary>
    public class BsaFile
    {
        private Header Header { get; set; }

        /// <summary>
        /// Folder hash to folder record map.
        /// </summary>
        private Dictionary<long, FolderRecord> Folders { get; set; } = new();

        private readonly BinaryReader _binaryReader;

        public BsaFile(BinaryReader binaryReader)
        {
            _binaryReader = binaryReader;
            InitBsaFile();
        }

        private void InitBsaFile()
        {
            Header = Header.Parse(_binaryReader);
            for (var i = 0; i < Header.FolderCount; i++)
            {
                var folderRecord = FolderRecord.Parse(_binaryReader, Header);
                Folders[folderRecord.Hash] = folderRecord;
            }
        }


        public bool CheckIfFileExists(string fullFileName)
        {
            fullFileName = ConvertFileName(fullFileName);
            try
            {
                var pathSeparatorIndex = fullFileName.LastIndexOf('\\');
                var folderName = fullFileName[..pathSeparatorIndex];
                var folderHash = HashCalculator.GetHashCode(folderName, true);
                var folder = Folders[folderHash];

                if (folder == null) return false;
                if (folder.Files == null)
                {
                    LoadFolder(folderHash);
                }

                var fileName = fullFileName[(pathSeparatorIndex + 1).GetHashCode().GetHashCode()..];
                var fileHash = HashCalculator.GetHashCode(fileName, false);
                return folder.Files!.Files[fileHash] != null;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public MemoryStream GetFile(string fullFileName)
        {
            fullFileName = ConvertFileName(fullFileName);

            try
            {
                var pathSeparatorIndex = fullFileName.LastIndexOf('\\');
                var folderName = fullFileName[..pathSeparatorIndex];
                var folderHash = HashCalculator.GetHashCode(folderName, true);
                var folder = Folders[folderHash];

                if (folder == null) throw new InvalidDataException($@"Folder {folderName} does not exist");
                if (folder.Files == null)
                {
                    LoadFolder(folderHash);
                }

                var fileName = fullFileName[(pathSeparatorIndex + 1).GetHashCode().GetHashCode()..];
                var fileHash = HashCalculator.GetHashCode(fileName, false);
                if (folder.Files!.Files[fileHash] == null)
                    throw new InvalidDataException($@"File {fileName} does not exist inside {folderName}");
                var fileBytes = ReadFile(folder.Files.Files[fileHash]);
                return new MemoryStream(fileBytes, false);
            }
            catch (Exception e)
            {
                Console.WriteLine($@"Archive exception for filename {fullFileName}: {e}");
            }

            return null;
        }

        private static string ConvertFileName(string fullFileName)
        {
            fullFileName = fullFileName.ToLowerInvariant();
            fullFileName = fullFileName.Trim();
            if (fullFileName.IndexOf("/", StringComparison.Ordinal) == -1) return fullFileName;
            var fileNameBuilder = new StringBuilder(fullFileName, fullFileName.Length);
            fileNameBuilder.Replace('/', '\\');
            fullFileName = fileNameBuilder.ToString();

            return fullFileName;
        }

        private void LoadFolder(long folderHash)
        {
            var folder = Folders[folderHash];
            var fileRecordBlock = FileRecordBlock.Parse(_binaryReader, folder, Header);
            folder.Files = fileRecordBlock;
        }

        private byte[] ReadFile(FileRecord fileRecord)
        {
            _binaryReader.BaseStream.Seek(fileRecord.Offset, SeekOrigin.Begin);
            if (Header.ArchiveFlags.Contains(ArchiveFlag.EmbedFileNames))
            {
                var nameSize = _binaryReader.ReadByte();
                var name = new string(_binaryReader.ReadChars(nameSize));
            }

            if (fileRecord.IsCompressed)
            {
                var originalSize = _binaryReader.ReadUInt32();
                switch (Header.Version)
                {
                    case 0x68:
                    {
                        //zLib
                        var compressedData = _binaryReader.ReadBytes(checked((int)fileRecord.Size));
                        var decompressedData = new byte[originalSize];
                        using var compressedDataStream = new MemoryStream(compressedData, false);
                        using var decompressStream = new ZlibStream(compressedDataStream, CompressionMode.Decompress);
                        var readAmount = decompressStream.Read(decompressedData, 0, checked((int)originalSize));
                        if (readAmount != originalSize)
                        {
                            Debug.LogError("Decompressed file size doesn't match with the original decompressed size");
                        }

                        return decompressedData;
                    }
                    case 0x69:
                        //LZ4
                        throw new NotImplementedException("Skyrim SE archives are not supported yet");
                    default:
                        throw new NotImplementedException($@"Unsupported archive version: {Header.Version}");
                }
            }
            else
            {
                return _binaryReader.ReadBytes(checked((int)fileRecord.Size));
            }
        }
    }
}