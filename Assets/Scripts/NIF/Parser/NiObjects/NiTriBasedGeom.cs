using System.IO;
using NIF.Parser.NiObjects.Enums;
using NIF.Parser.NiObjects.Structures;

namespace NIF.Parser.NiObjects
{
    /// <summary>
    /// Describes a mesh, built from triangles.
    /// </summary>
    public class NiTriBasedGeom : NiGeometry
    {
        protected NiTriBasedGeom(BsLightingShaderType shaderType, string name, uint extraDataListLength,
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

        protected new static NiTriBasedGeom Parse(BinaryReader nifReader, string ownerObjectName, Header header)
        {
            var ancestor = NiGeometry.Parse(nifReader, ownerObjectName, header);
            return new NiTriBasedGeom(ancestor.ShaderType, ancestor.Name, ancestor.ExtraDataListLength,
                ancestor.ExtraDataListReferences, ancestor.ControllerObjectReference, ancestor.Flags,
                ancestor.Translation, ancestor.Rotation, ancestor.Scale, ancestor.PropertiesNumber,
                ancestor.PropertiesReferences, ancestor.CollisionObjectReference, ancestor.BoundingSphere,
                ancestor.BoundMinMax, ancestor.SkinReference, ancestor.DataReference, ancestor.SkinInstanceReference,
                ancestor.MaterialData, ancestor.ShaderPropertyReference, ancestor.AlphaPropertyReference);
        }
    }
}