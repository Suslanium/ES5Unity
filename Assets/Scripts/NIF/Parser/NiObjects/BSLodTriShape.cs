using System.IO;

namespace NIF.Parser.NiObjects
{
    /// <summary>
    /// A variation on NiTriShape, for visibility control over vertex groups.
    /// </summary>
    public class BsLodTriShape : NiTriBasedGeom
    {
        public uint LOD0Size { get; private set; }

        public uint LOD1Size { get; private set; }

        public uint LOD2Size { get; private set; }

        public BsLodTriShape(NiTriBasedGeom ancestor) : base(ancestor.ShaderType, ancestor.Name, ancestor.ExtraDataListLength,
            ancestor.ExtraDataListReferences, ancestor.ControllerObjectReference, ancestor.Flags,
            ancestor.Translation, ancestor.Rotation, ancestor.Scale, ancestor.PropertiesNumber,
            ancestor.PropertiesReferences, ancestor.CollisionObjectReference, ancestor.BoundingSphere,
            ancestor.BoundMinMax, ancestor.SkinReference, ancestor.DataReference, ancestor.SkinInstanceReference,
            ancestor.MaterialData, ancestor.ShaderPropertyReference, ancestor.AlphaPropertyReference)
        {
        }

        public new static BsLodTriShape Parse(BinaryReader nifReader, string ownerObjectName, Header header)
        {
            var ancestor = NiTriBasedGeom.Parse(nifReader, ownerObjectName, header);
            var triShape = new BsLodTriShape(ancestor)
            {
                LOD0Size = nifReader.ReadUInt32(),
                LOD1Size = nifReader.ReadUInt32(),
                LOD2Size = nifReader.ReadUInt32()
            };
            return triShape;
        }
    }
}