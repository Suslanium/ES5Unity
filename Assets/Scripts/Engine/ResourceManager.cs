using System.Collections.Generic;
using System.IO;
using System.Linq;
using BSA;

namespace Engine
{
    public class ResourceManager
    {
        private readonly string _dataFolderPath;
        private readonly List<BsaFile> _archives = new();

        public ResourceManager(string dataFolderPath)
        {
            _dataFolderPath = dataFolderPath;
            var archivePaths = Directory.GetFiles(_dataFolderPath, "*.bsa", SearchOption.TopDirectoryOnly);
            foreach (var archivePath in archivePaths)
            {
                _archives.Add(new BsaFile(new BinaryReader(File.Open(archivePath, FileMode.Open))));
            }
        }

        public MemoryStream GetFileOrNull(string resourcePath)
        {
            return (from archive in _archives
                where archive.CheckIfFileExists(resourcePath)
                select archive.GetFile(resourcePath)).FirstOrDefault();
        }

        /// <summary>
        /// Call this only when exiting the game
        /// </summary>
        public void Close()
        {
            foreach (var archive in _archives)
            {
                archive.Close();
            }
        }
    }
}