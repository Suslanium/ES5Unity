using System.IO;

namespace NIF.Parser.NiObjects
{
    /// <summary>
    /// A shape node that refers to singular triangle data.
    /// </summary>
    public class NiTriShape : NiTriBasedGeom
    {
        public NiTriShape(NiTriBasedGeom ancestor) : base(ancestor.ShaderType, ancestor.Name,
            ancestor.ExtraDataListLength,
            ancestor.ExtraDataListReferences, ancestor.ControllerObjectReference, ancestor.Flags,
            ancestor.Translation, ancestor.Rotation, ancestor.Scale, ancestor.PropertiesNumber,
            ancestor.PropertiesReferences, ancestor.CollisionObjectReference, ancestor.BoundingSphere,
            ancestor.BoundMinMax, ancestor.SkinReference, ancestor.DataReference, ancestor.SkinInstanceReference,
            ancestor.MaterialData, ancestor.ShaderPropertyReference, ancestor.AlphaPropertyReference)
        {
        }

        public new static NiTriShape Parse(BinaryReader nifReader, string ownerObjectName, Header header)
        {
            var ancestor = NiTriBasedGeom.Parse(nifReader, ownerObjectName, header);
            return new NiTriShape(ancestor);
        }
    }
}