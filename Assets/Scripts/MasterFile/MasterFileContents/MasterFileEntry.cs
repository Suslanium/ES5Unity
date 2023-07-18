using System.IO;

namespace MasterFile.MasterFileContents
{
    /// <summary>
    /// Just an empty superclass.
    /// </summary>
    public abstract class MasterFileEntry
    {
        public static MasterFileEntry Parse(BinaryReader fileReader, long position)
        {
            fileReader.BaseStream.Seek(position, SeekOrigin.Begin);
            string entryType = new string(fileReader.ReadChars(4));
            if (entryType.Equals("GRUP"))
            {
                return Group.Parse(fileReader, fileReader.BaseStream.Position);
            }
            else
            {
                return Record.Parse(entryType, fileReader, fileReader.BaseStream.Position);
            }
        }
    }
}