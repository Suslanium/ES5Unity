using System.IO;
using NIF.Structures;

namespace NIF
{
    public class NIFReader
    {
        public static NIFile ReadNIF(BinaryReader nifReader, long startPosition)
        {
            nifReader.BaseStream.Seek(startPosition, SeekOrigin.Begin);
            Header header = Header.ParseHeader(nifReader);
            return new NIFile(header);
        }
    }
}