using System.IO;

namespace NIF.Parser.NiObjects.Structures
{
    /// <summary>
    /// List of three vertex indices.
    /// </summary>
    public class Triangle
    {
        /// <summary>
        /// First vertex index.
        /// </summary>
        public ushort V1 { get; private set; }
        
        /// <summary>
        /// Second vertex index.
        /// </summary>
        public ushort V2 { get; private set; }
        
        /// <summary>
        /// Third vertex index.
        /// </summary>
        public ushort V3 { get; private set; }

        private Triangle() {}

        public Triangle(ushort v1, ushort v2, ushort v3)
        {
            V1 = v1;
            V2 = v2;
            V3 = v3;
        }

        public static Triangle Parse(BinaryReader binaryReader)
        {
            var tri = new Triangle
            {
                V1 = binaryReader.ReadUInt16(),
                V2 = binaryReader.ReadUInt16(),
                V3 = binaryReader.ReadUInt16()
            };
            return tri;
        }
    }
}