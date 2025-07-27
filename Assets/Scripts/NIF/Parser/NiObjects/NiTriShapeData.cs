using System.IO;
using NIF.Parser.NiObjects.Structures;

namespace NIF.Parser.NiObjects
{
    /// <summary>
    /// Holds mesh data using a list of singular triangles.
    /// </summary>
    public class NiTriShapeData : NiTriBasedGeomData
    {
        /// <summary>
        /// Num Triangles times 3.
        /// </summary>
        public uint TrianglePointsNumber { get; private set; }

        /// <summary>
        /// Do we have triangle data?
        /// </summary>
        public bool HasTriangles { get; private set; }

        /// <summary>
        /// Triangle data.
        /// </summary>
        public Triangle[] Triangles { get; private set; }

        /// <summary>
        /// Number of shared normals groups.
        /// </summary>
        public ushort MatchGroupsNumber { get; private set; }

        /// <summary>
        /// The shared normals.
        /// </summary>
        public MatchGroup[] MatchGroups { get; private set; }

        private NiTriShapeData(NiTriBasedGeomData ancestor) : base(ancestor.GroupID, ancestor.VerticesNumber,
            ancestor.KeepFlags,
            ancestor.CompressFlags, ancestor.HasVertices, ancestor.Vertices, ancestor.DataFlags,
            ancestor.BsDataFlags, ancestor.MaterialCRC, ancestor.HasNormals, ancestor.Normals, ancestor.Tangents,
            ancestor.Bitangents, ancestor.BoundingSphere, ancestor.HasVertexColors, ancestor.VertexColors,
            ancestor.UVSets, ancestor.ConsistencyFlags, ancestor.AdditionalDataReference, ancestor.TrianglesNumber)
        {
        }

        public new static NiTriShapeData Parse(BinaryReader nifReader, string ownerObjectName, Header header)
        {
            var ancestor = NiTriBasedGeomData.Parse(nifReader, ownerObjectName, header);
            var triBasedGeomData = new NiTriShapeData(ancestor)
            {
                TrianglePointsNumber = nifReader.ReadUInt32(),
                HasTriangles = nifReader.ReadBoolean()
            };
            if (triBasedGeomData.HasTriangles)
            {
                triBasedGeomData.Triangles =
                    NifReaderUtils.ReadTriangleArray(nifReader, triBasedGeomData.TrianglesNumber);
            }

            triBasedGeomData.MatchGroupsNumber = nifReader.ReadUInt16();
            triBasedGeomData.MatchGroups =
                NifReaderUtils.ReadMatchGroupArray(nifReader, triBasedGeomData.MatchGroupsNumber);
            return triBasedGeomData;
        }
    }
}