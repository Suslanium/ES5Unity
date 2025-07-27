using System.IO;

namespace Engine.Resource.Source
{
    public abstract class ResourceSource
    {
        /// <summary>
        /// Priority of the resource source.
        /// Higher priority sources are checked first.
        /// Lower values are higher priority.
        /// </summary>
        public abstract int Priority { get; }
        
        protected readonly string DataFolderPath;
        
        protected ResourceSource(string dataFolderPath)
        {
            DataFolderPath = dataFolderPath;
        }
        
        public abstract Stream GetResourceOrNull(string resourcePath);
        
        public abstract void Close();
    }
}