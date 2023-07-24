using System;
using System.IO;
using NIF.NiObjects;
using NIF.NiObjects.Structures;

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

        public static string ReadString(BinaryReader binaryReader, Header header)
        {
            if (header.Version <= 0x14000005)
            {
                return ReadSizedString(binaryReader);
            }
            else if (header.Version >= 0x14010003)
            {
                var stringIndex = binaryReader.ReadUInt32();
                return stringIndex == 4294967295 ? null : header.Strings[stringIndex];
            }

            return null;
        }

        public static string[] ReadStringArray(BinaryReader binaryReader, Header header, uint length)
        {
            var array = new string[length];
            for (var i = 0; i < length; i++)
            {
                array[i] = ReadString(binaryReader, header);
            }

            return array;
        }

        public static float[] ReadFloatArray(BinaryReader binaryReader, uint length)
        {
            var array = new float[length];
            for (var i = 0; i < length; i++)
            {
                array[i] = binaryReader.ReadSingle();
            }

            return array;
        }

        //These methods should have been done using generics, but I can't really do that because C# doesn't allow return type covariance
        public static Vector3[] ReadVector3Array(BinaryReader binaryReader, uint length)
        {
            var array = new Vector3[length];
            for (var i = 0; i < length; i++)
            {
                array[i] = Vector3.Parse(binaryReader);
            }

            return array;
        }

        public static Color4[] ReadColor4Array(BinaryReader binaryReader, uint length)
        {
            var array = new Color4[length];
            for (var i = 0; i < length; i++)
            {
                array[i] = Color4.Parse(binaryReader);
            }

            return array;
        }

        public static TexCoord[] ReadTexCoordArray(BinaryReader binaryReader, uint length)
        {
            var array = new TexCoord[length];
            for (var i = 0; i < length; i++)
            {
                array[i] = TexCoord.Parse(binaryReader);
            }

            return array;
        }

        public static Triangle[] ReadTriangleArray(BinaryReader binaryReader, uint length)
        {
            var array = new Triangle[length];
            for (var i = 0; i < length; i++)
            {
                array[i] = Triangle.Parse(binaryReader);
            }

            return array;
        }

        public static MatchGroup[] ReadMatchGroupArray(BinaryReader binaryReader, uint length)
        {
            var array = new MatchGroup[length];
            for (var i = 0; i < length; i++)
            {
                array[i] = MatchGroup.Parse(binaryReader);
            }

            return array;
        }

        public static int ReadRef(BinaryReader binaryReader)
        {
            return binaryReader.ReadInt32();
        }

        public static int[] ReadRefArray(BinaryReader binaryReader, uint length)
        {
            var refArray = new int[length];
            for (var i = 0; i < length; i++)
            {
                refArray[i] = binaryReader.ReadInt32();
            }

            return refArray;
        }

        public static float ReadHalfPrecisionFloat(BinaryReader binaryReader)
        {
            var bytes = binaryReader.ReadBytes(2);
            return GetHalfPrecisionFloat(bytes[0], bytes[1]);
        }

        //This was taken from Stackoverflow(https://stackoverflow.com/questions/37759848/convert-byte-array-to-16-bits-float)
        public static float GetHalfPrecisionFloat(byte HO, byte LO)
        {
            var intVal = BitConverter.ToInt32(new byte[] { HO, LO, 0, 0 }, 0);

            int mant = intVal & 0x03ff;
            int exp = intVal & 0x7c00;
            if (exp == 0x7c00) exp = 0x3fc00;
            else if (exp != 0)
            {
                exp += 0x1c000;
                if (mant == 0 && exp > 0x1c400)
                    return BitConverter.ToSingle(BitConverter.GetBytes((intVal & 0x8000) << 16 | exp << 13 | 0x3ff), 0);
            }
            else if (mant != 0)
            {
                exp = 0x1c400;
                do
                {
                    mant <<= 1;
                    exp -= 0x400;
                } while ((mant & 0x400) == 0);

                mant &= 0x3ff;
            }

            return BitConverter.ToSingle(BitConverter.GetBytes((intVal & 0x8000) << 16 | (exp | mant) << 13), 0);
        }
    }
}