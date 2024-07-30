using System.Collections.Generic;
using System.IO;
using System.Linq;
using BSA;

namespace Engine.Resource.Source
{
    public class ArchiveResourceSource : ResourceSource
    {
        public override int Priority => 1;
        
        private readonly List<BsaFile> _archives = new();
        
        public ArchiveResourceSource(string dataFolderPath) : base(dataFolderPath)
        {
            var archivePaths = Directory.GetFiles(dataFolderPath, "*.bsa", SearchOption.TopDirectoryOnly);
            foreach (var archivePath in archivePaths)
            {
                _archives.Add(new BsaFile(new BinaryReader(File.Open(archivePath, FileMode.Open))));
            }
        }

        public override Stream GetResourceOrNull(string resourcePath)
        {
            return (from archive in _archives
                where archive.CheckIfFileExists(resourcePath)
                select archive.GetFile(resourcePath)).FirstOrDefault();
        }

        public override void Close()
        {
            foreach (var archive in _archives)
            {
                archive.Close();
            }
        }
    }
}