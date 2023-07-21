using System.IO;

namespace NIF
{
    public class NIFReaderUtils
    {
        public static string ReadExportString(BinaryReader binaryReader)
        {
            var stringSize = binaryReader.ReadByte();
            return new string(binaryReader.ReadChars(stringSize));
        }

        public static string ReadSizedString(BinaryReader binaryReader)
        {
            var stringSize = binaryReader.ReadUInt32();
            return new string(binaryReader.ReadChars((checked((int)stringSize))));
        }

        public static string[] ReadSizedStringArray(BinaryReader binaryReader, uint length)
        {
            string[] array = new string[length];
            for (var i = 0; i < length; i++)
            {
                array[i] = ReadSizedString(binaryReader);
            }

            return array;
        }

        public static ushort[] ReadUshortArray(BinaryReader binaryReader, uint length)
        {
            ushort[] array = new ushort[length];
            for (var i = 0; i < length; i++)
            {
                array[i] = binaryReader.ReadUInt16();
            }

            return array;
        }
        
        public static uint[] ReadUintArray(BinaryReader binaryReader, uint length)
        {
            uint[] array = new uint[length];
            for (var i = 0; i < length; i++)
            {
                array[i] = binaryReader.ReadUInt32();
            }

            return array;
        }
    }
}