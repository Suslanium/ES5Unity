using System.IO;
using NIF.NiObjects.Enums;
using NIF.NiObjects.Structures;

namespace NIF.NiObjects
{
    /// <summary>
    /// A shape node that refers to singular triangle data.
    /// </summary>
    public class NiTriShape : NiTriBasedGeom
    {
        public NiTriShape(BsLightingShaderType shaderType, string name, uint extraDataListLength,
            int[] extraDataListReferences, int controllerObjectReference, uint flags, Vector3 translation,
            Matrix33 rotation, float scale, uint propertiesNumber, int[] propertiesReferences,
            int collisionObjectReference, NiBound boundingSphere, float[] boundMinMax, int skinReference,
            int dataReference, int skinInstanceReference, MaterialData materialData, int shaderPropertyReference,
            int alphaPropertyReference) : base(shaderType, name, extraDataListLength, extraDataListReferences,
            controllerObjectReference, flags, translation, rotation, scale, propertiesNumber, propertiesReferences,
            collisionObjectReference, boundingSphere, boundMinMax, skinReference, dataReference, skinInstanceReference,
            materialData, shaderPropertyReference, alphaPropertyReference)
        {
        }

        public new static NiTriShape Parse(BinaryReader nifReader, string ownerObjectName, Header header)
        {
            var ancestor = NiTriBasedGeom.Parse(nifReader, ownerObjectName, header);
            return new NiTriShape(ancestor.ShaderType, ancestor.Name, ancestor.ExtraDataListLength,
                ancestor.ExtraDataListReferences, ancestor.ControllerObjectReference, ancestor.Flags,
                ancestor.Translation, ancestor.Rotation, ancestor.Scale, ancestor.PropertiesNumber,
                ancestor.PropertiesReferences, ancestor.CollisionObjectReference, ancestor.BoundingSphere,
                ancestor.BoundMinMax, ancestor.SkinReference, ancestor.DataReference, ancestor.SkinInstanceReference,
                ancestor.MaterialData, ancestor.ShaderPropertyReference, ancestor.AlphaPropertyReference);
        }
    }
}