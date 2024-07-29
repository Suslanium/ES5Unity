using System.IO;
using NIF.Parser.NiObjects.Enums;
using NIF.Parser.NiObjects.Structures;

namespace NIF.Parser.NiObjects
{
    /// <summary>
    /// A convex shape built from vertices. 
    /// </summary>
    public class BhkConvexVerticesShape: BhkConvexShape
    {
        public uint NumVertices { get; private set; }
        
        public Vector4[] Vertices { get; private set; }
        
        /// <summary>
        /// The number of half spaces.
        /// </summary>
        public uint NumNormals { get; private set; }
        
        /// <summary>
        /// Half spaces as determined by the set of vertices above. First three components define the normal pointing to the exterior, fourth component is the signed distance of the separating plane to the origin: it is minus the dot product of v and n, where v is any vertex on the separating plane, and n is the normal. Lexicographically sorted.
        /// </summary>
        public Vector4[] Normals { get; private set; }
        
        private BhkConvexVerticesShape(SkyrimHavokMaterial material, float radius) : base(material, radius)
        {
        }
        
        public new static BhkConvexVerticesShape Parse(BinaryReader nifReader, string ownerObjectName, Header header)
        {
            var ancestor = BhkConvexShape.Parse(nifReader, ownerObjectName, header);
            var bhkConvexVerticesShape = new BhkConvexVerticesShape(ancestor.Material, ancestor.Radius);
            nifReader.BaseStream.Seek(24, SeekOrigin.Current);
            bhkConvexVerticesShape.NumVertices = nifReader.ReadUInt32();
            bhkConvexVerticesShape.Vertices = new Vector4[bhkConvexVerticesShape.NumVertices];
            for (var i = 0; i < bhkConvexVerticesShape.NumVertices; i++)
            {
                bhkConvexVerticesShape.Vertices[i] = Vector4.Parse(nifReader);
            }
            bhkConvexVerticesShape.NumNormals = nifReader.ReadUInt32();
            bhkConvexVerticesShape.Normals = new Vector4[bhkConvexVerticesShape.NumNormals];
            for (var i = 0; i < bhkConvexVerticesShape.NumNormals; i++)
            {
                bhkConvexVerticesShape.Normals[i] = Vector4.Parse(nifReader);
            }
            return bhkConvexVerticesShape;
        }
    }
}