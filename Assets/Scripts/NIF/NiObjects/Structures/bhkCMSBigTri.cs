using System.IO;

namespace NIF.NiObjects.Structures
{
    /// <summary>
    /// Bethesda extension of hkpCompressedMeshShape::BigTriangle. Triangles that don't fit the maximum size.
    /// </summary>
    public class BhkCmsBigTri
    {
        /// <summary>
        /// First vertex index.
        /// </summary>
        public ushort Vertex1 { get; private set; }
        
        /// <summary>
        /// Second vertex index.
        /// </summary>
        public ushort Vertex2 { get; private set; }
        
        /// <summary>
        /// Third vertex index.
        /// </summary>
        public ushort Vertex3 { get; private set; }
        
        public uint Material { get; private set; }
        
        public ushort WeldingInfo { get; private set; }

        private BhkCmsBigTri()
        {
        }
        
        public static BhkCmsBigTri Parse(BinaryReader nifReader)
        {
            var cmsBigTri = new BhkCmsBigTri
            {
                Vertex1 = nifReader.ReadUInt16(),
                Vertex2 = nifReader.ReadUInt16(),
                Vertex3 = nifReader.ReadUInt16(),
                Material = nifReader.ReadUInt32(),
                WeldingInfo = nifReader.ReadUInt16()
            };
            return cmsBigTri;
        }
    }
}