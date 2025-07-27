using System.IO;
using NIF.Parser.NiObjects.Enums;
using NIF.Parser.NiObjects.Structures;

namespace NIF.Parser.NiObjects
{
    /// <summary>
    /// Describes a visible scene element with vertices like a mesh, a particle system, lines, etc.
    /// </summary>
    public class NiGeometry : NiAvObject
    {
        public NiBound BoundingSphere { get; private set; }

        public float[] BoundMinMax { get; private set; }

        public int SkinReference { get; private set; }

        public int DataReference { get; private set; }

        public int SkinInstanceReference { get; private set; }

        public MaterialData MaterialData { get; private set; }

        public int ShaderPropertyReference { get; private set; }

        public int AlphaPropertyReference { get; private set; }

        private NiGeometry(BsLightingShaderType shaderType, string name, uint extraDataListLength,
            int[] extraDataListReferences, int controllerObjectReference, uint flags, Vector3 translation,
            Matrix33 rotation, float scale, uint propertiesNumber, int[] propertiesReferences,
            int collisionObjectReference) : base(shaderType, name, extraDataListLength, extraDataListReferences,
            controllerObjectReference, flags, translation, rotation, scale, propertiesNumber, propertiesReferences,
            collisionObjectReference)
        {
        }

        protected NiGeometry(BsLightingShaderType shaderType, string name, uint extraDataListLength,
            int[] extraDataListReferences, int controllerObjectReference, uint flags, Vector3 translation,
            Matrix33 rotation, float scale, uint propertiesNumber, int[] propertiesReferences,
            int collisionObjectReference, NiBound boundingSphere, float[] boundMinMax, int skinReference,
            int dataReference, int skinInstanceReference, MaterialData materialData, int shaderPropertyReference,
            int alphaPropertyReference) : base(shaderType, name, extraDataListLength, extraDataListReferences,
            controllerObjectReference, flags, translation, rotation, scale, propertiesNumber, propertiesReferences,
            collisionObjectReference)
        {
            BoundingSphere = boundingSphere;
            BoundMinMax = boundMinMax;
            SkinReference = skinReference;
            DataReference = dataReference;
            SkinInstanceReference = skinInstanceReference;
            MaterialData = materialData;
            ShaderPropertyReference = shaderPropertyReference;
            AlphaPropertyReference = alphaPropertyReference;
        }

        protected new static NiGeometry Parse(BinaryReader nifReader, string ownerObjectName, Header header)
        {
            var ancestor = NiAvObject.Parse(nifReader, ownerObjectName, header);
            var niGeometry = new NiGeometry(ancestor.ShaderType, ancestor.Name, ancestor.ExtraDataListLength,
                ancestor.ExtraDataListReferences, ancestor.ControllerObjectReference, ancestor.Flags,
                ancestor.Translation, ancestor.Rotation, ancestor.Scale, ancestor.PropertiesNumber,
                ancestor.PropertiesReferences, ancestor.CollisionObjectReference);
            if (header.Version == 0x14020007 && Conditions.BsGteSse(header) && ownerObjectName == "NiParticleSystem")
            {
                niGeometry.BoundingSphere = NiBound.Parse(nifReader);
                if (Conditions.BsF76(header))
                {
                    niGeometry.BoundMinMax = NifReaderUtils.ReadFloatArray(nifReader, 6);
                }

                niGeometry.SkinReference = NifReaderUtils.ReadRef(nifReader);
            }

            if (Conditions.NiBsLtSse(header))
            {
                niGeometry.DataReference = NifReaderUtils.ReadRef(nifReader);
                niGeometry.SkinInstanceReference = NifReaderUtils.ReadRef(nifReader);
                niGeometry.MaterialData = MaterialData.Parse(nifReader, header);
            }
            else if (header.Version == 0x14020007 && Conditions.BsGteSse(header) &&
                     ownerObjectName != "NiParticleSystem")
            {
                niGeometry.DataReference = NifReaderUtils.ReadRef(nifReader);
                niGeometry.SkinInstanceReference = NifReaderUtils.ReadRef(nifReader);
                niGeometry.MaterialData = MaterialData.Parse(nifReader, header);
            }

            if (header.Version != 0x14020007 || Conditions.NiBsLteFo3(header)) return niGeometry;
            niGeometry.ShaderPropertyReference = NifReaderUtils.ReadRef(nifReader);
            niGeometry.AlphaPropertyReference = NifReaderUtils.ReadRef(nifReader);


            return niGeometry;
        }
    }
}