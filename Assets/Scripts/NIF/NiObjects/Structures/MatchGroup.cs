using System.IO;

namespace NIF.NiObjects.Structures
{
    /// <summary>
    /// Group of vertex indices of vertices that match.
    /// </summary>
    public class MatchGroup
    {
        /// <summary>
        /// Number of vertices in this group.
        /// </summary>
        public ushort VerticesNumber { get; private set; }

        /// <summary>
        /// The vertex indices.
        /// </summary>
        public ushort[] VertexIndices { get; private set; }

        private MatchGroup()
        {
        }

        public MatchGroup(ushort verticesNumber, ushort[] vertexIndices)
        {
            VerticesNumber = verticesNumber;
            VertexIndices = vertexIndices;
        }

        public static MatchGroup Parse(BinaryReader binaryReader)
        {
            var group = new MatchGroup
            {
                VerticesNumber = binaryReader.ReadUInt16()
            };
            group.VertexIndices = NIFReaderUtils.ReadUshortArray(binaryReader, group.VerticesNumber);
            return group;
        }
    }
}