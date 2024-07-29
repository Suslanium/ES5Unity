using System.IO;
using NIF.Parser.NiObjects.Structures;

namespace NIF.Parser.NiObjects
{
    /// <summary>
    /// A compressed mesh shape for collision in Skyrim.
    /// </summary>
    public class BhkCompressedMeshShapeData: BhkRefObject
    {
        /// <summary>
        /// Number of bits in the shape-key reserved for a triangle index
        /// </summary>
        public uint NumBitsPerIndex { get; private set; }
        
        /// <summary>
        /// Number of bits in the shape-key reserved for a triangle index and its winding
        /// </summary>
        public uint NumBitsPerWIndex { get; private set; }
        
        /// <summary>
        /// Mask used to get the triangle index and winding from a shape-key
        /// </summary>
        public uint MaskWIndex { get; private set; }
        
        /// <summary>
        /// Mask used to get the triangle index from a shape-key.
        /// </summary>
        public uint MaskIndex { get; private set; }
        
        /// <summary>
        /// Quantization error.
        /// </summary>
        public float Error { get; private set; }
        
        /// <summary>
        /// Coordinates of the corner with the lowest numerical values.
        /// </summary>
        public Vector4 Min { get; private set; }
        
        /// <summary>
        /// Coordinates of the corner with the highest numerical values.
        /// </summary>
        public Vector4 Max { get; private set; }
        
        public byte HkWeldingType { get; private set; }
        
        public byte MaterialType { get; private set; }
        
        public uint NumMaterials { get; private set; }
        
        /// <summary>
        /// Materials used by Chunks. Chunks refer to this table by index.
        /// </summary>
        public BhkMeshMaterial[] ChunkMaterials { get; private set; }
        
        public uint NumTransforms { get; private set; }
        
        /// <summary>
        /// Transforms used by Chunks. Chunks refer to this table by index.
        /// </summary>
        public BhkQsTransform[] ChunkTransforms { get; private set; }
        
        public uint NumBigVerts { get; private set; }
        
        /// <summary>
        /// Vertices paired with Big Tris (triangles that are too large for chunks)
        /// </summary>
        public Vector4[] BigVerts { get; private set; }
        
        public uint NumBigTris { get; private set; }
        
        /// <summary>
        /// Triangles that are too large to fit in a chunk.
        /// </summary>
        public BhkCmsBigTri[] BigTris { get; private set; }
        
        public uint NumChunks { get; private set; }
        
        public BhkCmsChunk[] Chunks { get; private set; }

        private BhkCompressedMeshShapeData()
        {
        }

        public static BhkCompressedMeshShapeData Parse(BinaryReader nifReader, string ownerObjectName, Header header)
        {
            var bhkCompressedShapeData = new BhkCompressedMeshShapeData
            {
                NumBitsPerIndex = nifReader.ReadUInt32(),
                NumBitsPerWIndex = nifReader.ReadUInt32(),
                MaskWIndex = nifReader.ReadUInt32(),
                MaskIndex = nifReader.ReadUInt32(),
                Error = nifReader.ReadSingle(),
                Min = Vector4.Parse(nifReader),
                Max = Vector4.Parse(nifReader),
                HkWeldingType = nifReader.ReadByte(),
                MaterialType = nifReader.ReadByte()
            };
            var numMaterials32 = nifReader.ReadUInt32();
            nifReader.BaseStream.Seek(numMaterials32 * 4, SeekOrigin.Current);
            var numMaterials16 = nifReader.ReadUInt32();
            nifReader.BaseStream.Seek(numMaterials16 * 4, SeekOrigin.Current);
            var numMaterials8 = nifReader.ReadUInt32();
            nifReader.BaseStream.Seek(numMaterials8 * 4, SeekOrigin.Current);
            bhkCompressedShapeData.NumMaterials = nifReader.ReadUInt32();
            bhkCompressedShapeData.ChunkMaterials = new BhkMeshMaterial[bhkCompressedShapeData.NumMaterials];
            for (var i = 0; i < bhkCompressedShapeData.ChunkMaterials.Length; i++)
            {
                bhkCompressedShapeData.ChunkMaterials[i] = BhkMeshMaterial.Parse(nifReader, ownerObjectName, header);
            }
            nifReader.BaseStream.Seek(4, SeekOrigin.Current);
            bhkCompressedShapeData.NumTransforms = nifReader.ReadUInt32();
            bhkCompressedShapeData.ChunkTransforms = new BhkQsTransform[bhkCompressedShapeData.NumTransforms];
            for (var i = 0; i < bhkCompressedShapeData.ChunkTransforms.Length; i++)
            {
                bhkCompressedShapeData.ChunkTransforms[i] = BhkQsTransform.Parse(nifReader);
            }
            bhkCompressedShapeData.NumBigVerts = nifReader.ReadUInt32();
            bhkCompressedShapeData.BigVerts = new Vector4[bhkCompressedShapeData.NumBigVerts];
            for (var i = 0; i < bhkCompressedShapeData.BigVerts.Length; i++)
            {
                bhkCompressedShapeData.BigVerts[i] = Vector4.Parse(nifReader);
            }
            bhkCompressedShapeData.NumBigTris = nifReader.ReadUInt32();
            bhkCompressedShapeData.BigTris = new BhkCmsBigTri[bhkCompressedShapeData.NumBigTris];
            for (var i = 0; i < bhkCompressedShapeData.BigTris.Length; i++)
            {
                bhkCompressedShapeData.BigTris[i] = BhkCmsBigTri.Parse(nifReader);
            }
            bhkCompressedShapeData.NumChunks = nifReader.ReadUInt32();
            bhkCompressedShapeData.Chunks = new BhkCmsChunk[bhkCompressedShapeData.NumChunks];
            for (var i = 0; i < bhkCompressedShapeData.Chunks.Length; i++)
            {
                bhkCompressedShapeData.Chunks[i] = BhkCmsChunk.Parse(nifReader);
            }
            nifReader.BaseStream.Seek(4, SeekOrigin.Current);
            return bhkCompressedShapeData;
        }
    }
}