using System.IO;

namespace MasterFile.MasterFileContents
{
    /// <summary>
    /// Just an empty superclass.
    /// </summary>
    public abstract class MasterFileEntry
    {
        public MasterFileEntry Parse(BinaryReader fileReader, ulong position)
        {
            return ParseFromFile(fileReader, position);
        }

        protected abstract MasterFileEntry ParseFromFile(BinaryReader fileReader, ulong position);
    }
}