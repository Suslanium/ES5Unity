using System.IO;
using NIF.Parser.NiObjects.Enums;
using NIF.Parser.NiObjects.Structures;

namespace NIF.Parser.NiObjects
{
    /// <summary>
    /// Mesh data: vertices, vertex normals, etc.(abstract)
    /// </summary>
    public class NiGeometryData : NiObject
    {
        /// <summary>
        /// Always zero.
        /// </summary>
        public int GroupID { get; private set; }

        /// <summary>
        /// Number of vertices.
        /// </summary>
        public ushort VerticesNumber { get; private set; }

        /// <summary>
        /// Used with NiCollision objects when OBB or TRI is set.
        /// </summary>
        public byte KeepFlags { get; private set; }

        public byte CompressFlags { get; private set; }

        /// <summary>
        /// Is the vertex array present? (Always non-zero.)
        /// </summary>
        public bool HasVertices { get; private set; }

        /// <summary>
        /// The mesh vertices.
        /// </summary>
        public Vector3[] Vertices { get; private set; }

        public ushort DataFlags { get; private set; }

        public ushort BsDataFlags { get; private set; }

        public uint MaterialCRC { get; private set; }

        /// <summary>
        /// Do we have lighting normals? These are essential for proper lighting: if not present, the model will only be influenced by ambient light.
        /// </summary>
        public bool HasNormals { get; private set; }

        /// <summary>
        /// The lighting normals.
        /// </summary>
        public Vector3[] Normals { get; private set; }

        /// <summary>
        /// Tangent vectors.
        /// </summary>
        public Vector3[] Tangents { get; private set; }

        /// <summary>
        /// Bitangent vectors.
        /// </summary>
        public Vector3[] Bitangents { get; private set; }

        public NiBound BoundingSphere { get; private set; }

        /// <summary>
        /// Do we have vertex colors? These are usually used to fine-tune the lighting of the model.
        /// </summary>
        public bool HasVertexColors { get; private set; }

        /// <summary>
        /// The vertex colors.
        /// </summary>
        public Color4[] VertexColors { get; private set; }

        /// <summary>
        /// The UV texture coordinates. They follow the OpenGL standard: some programs may require you to flip the second coordinate.
        /// </summary>
        public TexCoord[,] UVSets { get; private set; }

        /// <summary>
        /// Consistency Flags
        /// </summary>
        public ConsistencyType ConsistencyFlags { get; private set; }

        public int AdditionalDataReference { get; private set; }

        private NiGeometryData()
        {
        }

        protected NiGeometryData(int groupID, ushort verticesNumber, byte keepFlags, byte compressFlags, bool hasVertices,
            Vector3[] vertices, ushort dataFlags, ushort bsDataFlags, uint materialCRC, bool hasNormals,
            Vector3[] normals, Vector3[] tangents, Vector3[] bitangents, NiBound boundingSphere, bool hasVertexColors,
            Color4[] vertexColors, TexCoord[,] uvSets, ConsistencyType consistencyFlags, int additionalDataReference)
        {
            GroupID = groupID;
            VerticesNumber = verticesNumber;
            KeepFlags = keepFlags;
            CompressFlags = compressFlags;
            HasVertices = hasVertices;
            Vertices = vertices;
            DataFlags = dataFlags;
            BsDataFlags = bsDataFlags;
            MaterialCRC = materialCRC;
            HasNormals = hasNormals;
            Normals = normals;
            Tangents = tangents;
            Bitangents = bitangents;
            BoundingSphere = boundingSphere;
            HasVertexColors = hasVertexColors;
            VertexColors = vertexColors;
            UVSets = uvSets;
            ConsistencyFlags = consistencyFlags;
            AdditionalDataReference = additionalDataReference;
        }

        protected static NiGeometryData Parse(BinaryReader nifReader, string ownerObjectName, Header header)
        {
            var niGeomData = new NiGeometryData
            {
                GroupID = nifReader.ReadInt32()
            };
            if (ownerObjectName != "NiPSysData")
            {
                niGeomData.VerticesNumber = nifReader.ReadUInt16();
            }
            else if (Conditions.NiBsLtFo3(header))
            {
                niGeomData.VerticesNumber = nifReader.ReadUInt16();
            }
            else if (header.Version == 0x14020007 && Conditions.BsGteFo3(header))
            {
                niGeomData.VerticesNumber = nifReader.ReadUInt16();
            }

            niGeomData.KeepFlags = nifReader.ReadByte();
            niGeomData.CompressFlags = nifReader.ReadByte();

            niGeomData.HasVertices = nifReader.ReadBoolean();
            if (niGeomData.HasVertices)
            {
                niGeomData.Vertices = NifReaderUtils.ReadVector3Array(nifReader, niGeomData.VerticesNumber);
            }

            if (Conditions.Bs202(header))
            {
                niGeomData.BsDataFlags = nifReader.ReadUInt16();
            }
            else
            {
                niGeomData.DataFlags = nifReader.ReadUInt16();
            }

            if (header.Version == 0x14020007 && Conditions.BsGtFo3(header))
            {
                niGeomData.MaterialCRC = nifReader.ReadUInt32();
            }

            niGeomData.HasNormals = nifReader.ReadBoolean();
            if (niGeomData.HasNormals)
            {
                niGeomData.Normals = NifReaderUtils.ReadVector3Array(nifReader, niGeomData.VerticesNumber);
            }

            if (niGeomData.HasNormals && ((niGeomData.DataFlags | niGeomData.BsDataFlags) & 4096) != 0)
            {
                niGeomData.Tangents = NifReaderUtils.ReadVector3Array(nifReader, niGeomData.VerticesNumber);
                niGeomData.Bitangents = NifReaderUtils.ReadVector3Array(nifReader, niGeomData.VerticesNumber);
            }

            niGeomData.BoundingSphere = NiBound.Parse(nifReader);

            niGeomData.HasVertexColors = nifReader.ReadBoolean();
            if (niGeomData.HasVertexColors)
            {
                niGeomData.VertexColors = NifReaderUtils.ReadColor4Array(nifReader, niGeomData.VerticesNumber);
            }

            niGeomData.UVSets = new TexCoord[(niGeomData.DataFlags & 63) | (niGeomData.BsDataFlags & 1),
                niGeomData.VerticesNumber];
            for (var i = 0; i < ((niGeomData.DataFlags & 63) | (niGeomData.BsDataFlags & 1)); i++)
            {
                for (var j = 0; j < niGeomData.VerticesNumber; j++)
                {
                    niGeomData.UVSets[i, j] = TexCoord.Parse(nifReader);
                }
            }

            var consType = nifReader.ReadUInt16();
            niGeomData.ConsistencyFlags = consType switch
            {
                0x0000 => ConsistencyType.CtMutable,
                0x4000 => ConsistencyType.CtStatic,
                0x8000 => ConsistencyType.CtVolatile,
                _ => niGeomData.ConsistencyFlags
            };

            if (header.Version >= 0x14000004)
            {
                niGeomData.AdditionalDataReference = NifReaderUtils.ReadRef(nifReader);
            }

            return niGeomData;
        }
    }
}