using System.IO;
using BSA.Structures;

namespace BSA
{
    /// <summary>
    /// BSA files are the resource archive files used by Skyrim.
    /// </summary>
    public class BSAFile
    {
        public Header Header { get; private set; }
        
        private BSAFile() {}

        public static BSAFile InitBSAFile(BinaryReader binaryReader)
        {
            var file = new BSAFile
            {
                Header = Header.Parse(binaryReader)
            };
            return file;
        }
    }
}