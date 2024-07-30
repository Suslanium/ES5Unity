using System.IO;

namespace Engine.Resource.Source
{
    public class FileResourceSource : ResourceSource
    {
        public override int Priority => 0;
        
        public FileResourceSource(string dataFolderPath) : base(dataFolderPath)
        {
        }

        public override Stream GetResourceOrNull(string resourcePath)
        {
            var fullPath = Path.Combine(DataFolderPath, resourcePath);
            return !File.Exists(fullPath) ? null : File.Open(fullPath, FileMode.Open);
        }

        public override void Close()
        {
            //Nothing to do here, all the file streams should be closed manually
        }
    }
}