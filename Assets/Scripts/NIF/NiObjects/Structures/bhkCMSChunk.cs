using System.IO;

namespace NIF.NiObjects.Structures
{
    public class BhkCmsChunk
    {
        public Vector4 Translation { get; private set; }
        
        public uint MaterialIndex { get; private set; }
        
        public ushort Reference { get; private set; }
        
        public ushort TransformIndex { get; private set; }
        
        /// <summary>
        /// Number of vertices, multiplied by 3.
        /// </summary>
        public uint NumVertices { get; private set; }
        
        public UShortVector3[] Vertices { get; private set; }
        
        public uint NumIndices { get; private set; }
        
        public ushort[] Indices { get; private set; }
        
        public uint NumStrips { get; private set; }
        
        public ushort[] StripLengths { get; private set; }
        
        public uint NumWeldingInfo { get; private set; }
        
        public ushort[] WeldingInfo { get; private set; }

        private BhkCmsChunk()
        {
        }
        
        public static BhkCmsChunk Parse(BinaryReader nifReader)
        {
            var cmsChunk = new BhkCmsChunk
            {
                Translation = Vector4.Parse(nifReader),
                MaterialIndex = nifReader.ReadUInt32(),
                Reference = nifReader.ReadUInt16(),
                TransformIndex = nifReader.ReadUInt16(),
                NumVertices = nifReader.ReadUInt32()
            };
            cmsChunk.Vertices = new UShortVector3[cmsChunk.NumVertices / 3];
            for (var i = 0; i < cmsChunk.Vertices.Length; i++)
            {
                cmsChunk.Vertices[i] = UShortVector3.Parse(nifReader);
            }
            cmsChunk.NumIndices = nifReader.ReadUInt32();
            cmsChunk.Indices = new ushort[cmsChunk.NumIndices];
            for (var i = 0; i < cmsChunk.Indices.Length; i++)
            {
                cmsChunk.Indices[i] = nifReader.ReadUInt16();
            }
            cmsChunk.NumStrips = nifReader.ReadUInt32();
            cmsChunk.StripLengths = new ushort[cmsChunk.NumStrips];
            for (var i = 0; i < cmsChunk.StripLengths.Length; i++)
            {
                cmsChunk.StripLengths[i] = nifReader.ReadUInt16();
            }
            cmsChunk.NumWeldingInfo = nifReader.ReadUInt32();
            cmsChunk.WeldingInfo = new ushort[cmsChunk.NumWeldingInfo];
            for (var i = 0; i < cmsChunk.WeldingInfo.Length; i++)
            {
                cmsChunk.WeldingInfo[i] = nifReader.ReadUInt16();
            }
            return cmsChunk;
        }
    }
}