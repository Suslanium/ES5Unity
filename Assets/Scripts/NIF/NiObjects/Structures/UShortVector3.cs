using System.IO;

namespace NIF.NiObjects.Structures
{
    public class UShortVector3
    {
        public ushort X { get; private set; }
        
        public ushort Y { get; private set; }
        
        public ushort Z { get; private set; }
        
        private UShortVector3()
        {
        }
        
        public static UShortVector3 Parse(BinaryReader nifReader)
        {
            var ushortVector3 = new UShortVector3
            {
                X = nifReader.ReadUInt16(),
                Y = nifReader.ReadUInt16(),
                Z = nifReader.ReadUInt16()
            };
            return ushortVector3;
        }
        
        public Vector3 ToVector3()
        {
            return new Vector3(X, Y, Z);
        }
    }
}