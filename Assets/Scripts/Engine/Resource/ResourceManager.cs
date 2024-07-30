using System.Collections.Generic;
using System.IO;
using System.Linq;
using Engine.Resource.Source;

namespace Engine.Resource
{
    public class ResourceManager
    {
        private readonly List<ResourceSource> _sources = new();

        public ResourceManager(string dataFolderPath)
        {
            //TODO Replace this with DI or something
            _sources.Add(new ArchiveResourceSource(dataFolderPath));
            _sources.Add(new FileResourceSource(dataFolderPath));
            _sources.Sort((a, b) => a.Priority.CompareTo(b.Priority));
        }

        public Stream GetFileOrNull(string resourcePath)
        {
            return _sources.Select(source => source.GetResourceOrNull(resourcePath))
                .FirstOrDefault(stream => stream != null);
        }

        /// <summary>
        /// Call this only when exiting the game
        /// </summary>
        public void Close()
        {
            foreach (var source in _sources)
            {
                source.Close();
            }
        }
    }
}