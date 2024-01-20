using System.IO;

namespace NIF.NiObjects.Structures
{
    public class HkMatrix3
    {
        public float M11 { get; private set; }
        
        public float M12 { get; private set; }
        
        public float M13 { get; private set; }
        
        /// <summary>
        /// Unused
        /// </summary>
        public float M14 { get; private set; }
        
        public float M21 { get; private set; }
        
        public float M22 { get; private set; }
        
        public float M23 { get; private set; }
        
        /// <summary>
        /// Unused
        /// </summary>
        public float M24 { get; private set; }
        
        public float M31 { get; private set; }
        
        public float M32 { get; private set; }
        
        public float M33 { get; private set; }
        
        /// <summary>
        /// Unused
        /// </summary>
        public float M34 { get; private set; }

        private HkMatrix3()
        {
        }

        public static HkMatrix3 Parse(BinaryReader binaryReader)
        {
            return new HkMatrix3
            {
                M11 = binaryReader.ReadSingle(),
                M12 = binaryReader.ReadSingle(),
                M13 = binaryReader.ReadSingle(),
                M14 = binaryReader.ReadSingle(),
                M21 = binaryReader.ReadSingle(),
                M22 = binaryReader.ReadSingle(),
                M23 = binaryReader.ReadSingle(),
                M24 = binaryReader.ReadSingle(),
                M31 = binaryReader.ReadSingle(),
                M32 = binaryReader.ReadSingle(),
                M33 = binaryReader.ReadSingle(),
                M34 = binaryReader.ReadSingle()
            };
        }
    }
}