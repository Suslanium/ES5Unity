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
            var entryType = new string(fileReader.ReadChars(4));
            if (entryType.Equals("GRUP"))
            {
                return Group.Parse(fileReader);
            }
            else
            {
                return Record.Parse(entryType, fileReader, fileReader.BaseStream.Position);
            }
        }

        public static MasterFileEntry ReadHeaderAndSkip(BinaryReader fileReader, long position)
        {
            fileReader.BaseStream.Seek(position, SeekOrigin.Begin);
            var entryType = new string(fileReader.ReadChars(4));
            if (entryType.Equals("GRUP"))
            {
                return Group.ParseHeader(fileReader);
            }
            else
            {
                return Record.ParseHeaderAndSkip(entryType, fileReader, fileReader.BaseStream.Position);
            }
        }
    }
}