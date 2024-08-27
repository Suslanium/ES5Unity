using System;
using System.IO;
using Logger = Engine.Core.Logger;

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
            try
            {
                var fullPath = Path.Combine(DataFolderPath, resourcePath.Replace('\\', '/').TrimEnd('\0'));
                return !File.Exists(fullPath) ? null : File.Open(fullPath, FileMode.Open);
            }
            catch (Exception e)
            {
                Logger.LogError($"{resourcePath.Replace('\\', '/').TrimEnd('\0')} : {e.StackTrace}");
                return null;
            }
        }

        public override void Close()
        {
            //Nothing to do here, all the file streams should be closed manually
        }
    }
}