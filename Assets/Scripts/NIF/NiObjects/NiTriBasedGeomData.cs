using System.IO;
using NIF.NiObjects.Enums;
using NIF.NiObjects.Structures;

namespace NIF.NiObjects
{
    /// <summary>
    /// Describes a mesh, built from triangles.
    /// </summary>
    public class NiTriBasedGeomData : NiGeometryData
    {
        /// <summary>
        /// Number of triangles.
        /// </summary>
        public ushort TrianglesNumber { get; private set; }

        private NiTriBasedGeomData(int groupID, ushort verticesNumber, byte keepFlags, byte compressFlags,
            bool hasVertices, Vector3[] vertices, ushort dataFlags, ushort bsDataFlags, uint materialCRC,
            bool hasNormals, Vector3[] normals, Vector3[] tangents, Vector3[] bitangents, NiBound boundingSphere,
            bool hasVertexColors, Color4[] vertexColors, TexCoord[,] uvSets, ConsistencyType consistencyFlags,
            int additionalDataReference) : base(groupID, verticesNumber, keepFlags, compressFlags, hasVertices,
            vertices, dataFlags, bsDataFlags, materialCRC, hasNormals, normals, tangents, bitangents, boundingSphere,
            hasVertexColors, vertexColors, uvSets, consistencyFlags, additionalDataReference)
        {
        }

        protected NiTriBasedGeomData(int groupID, ushort verticesNumber, byte keepFlags, byte compressFlags,
            bool hasVertices, Vector3[] vertices, ushort dataFlags, ushort bsDataFlags, uint materialCRC,
            bool hasNormals, Vector3[] normals, Vector3[] tangents, Vector3[] bitangents, NiBound boundingSphere,
            bool hasVertexColors, Color4[] vertexColors, TexCoord[,] uvSets, ConsistencyType consistencyFlags,
            int additionalDataReference, ushort trianglesNumber) : base(groupID, verticesNumber, keepFlags,
            compressFlags, hasVertices, vertices, dataFlags, bsDataFlags, materialCRC, hasNormals, normals, tangents,
            bitangents, boundingSphere, hasVertexColors, vertexColors, uvSets, consistencyFlags,
            additionalDataReference)
        {
            TrianglesNumber = trianglesNumber;
        }

        protected new static NiTriBasedGeomData Parse(BinaryReader nifReader, string ownerObjectName, Header header)
        {
            var ancestor = NiGeometryData.Parse(nifReader, ownerObjectName, header);
            var triBasedGeomData = new NiTriBasedGeomData(ancestor.GroupID, ancestor.VerticesNumber, ancestor.KeepFlags,
                ancestor.CompressFlags, ancestor.HasVertices, ancestor.Vertices, ancestor.DataFlags,
                ancestor.BSDataFlags, ancestor.MaterialCRC, ancestor.HasNormals, ancestor.Normals, ancestor.Tangents,
                ancestor.Bitangents, ancestor.BoundingSphere, ancestor.HasVertexColors, ancestor.VertexColors,
                ancestor.UVSets, ancestor.ConsistencyFlags, ancestor.AdditionalDataReference)
            {
                TrianglesNumber = nifReader.ReadUInt16()
            };
            return triBasedGeomData;
        }
    }
}