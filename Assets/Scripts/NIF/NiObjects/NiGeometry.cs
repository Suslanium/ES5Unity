using System.IO;
using NIF.NiObjects.Enums;
using NIF.NiObjects.Structures;

namespace NIF.NiObjects
{
    /// <summary>
    /// Describes a visible scene element with vertices like a mesh, a particle system, lines, etc.
    /// </summary>
    public class NiGeometry : NiAVObject
    {
        public NiBound BoundingSphere { get; private set; }

        public float[] BoundMinMax { get; private set; }

        public int SkinReference { get; private set; }

        public int DataReference { get; private set; }

        public int SkinInstanceReference { get; private set; }

        public MaterialData MaterialData { get; private set; }

        public int ShaderPropertyReference { get; private set; }

        public int AlphaPropertyReference { get; private set; }

        private NiGeometry(BSLightingShaderType shaderType, string name, uint extraDataListLength,
            int[] extraDataListReferences, int controllerObjectReference, uint flags, Vector3 translation,
            Matrix33 rotation, float scale, uint propertiesNumber, int[] propertiesReferences,
            int collisionObjectReference) : base(shaderType, name, extraDataListLength, extraDataListReferences,
            controllerObjectReference, flags, translation, rotation, scale, propertiesNumber, propertiesReferences,
            collisionObjectReference)
        {
        }

        protected NiGeometry(BSLightingShaderType shaderType, string name, uint extraDataListLength,
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
            var ancestor = NiAVObject.Parse(nifReader, ownerObjectName, header);
            var niGeometry = new NiGeometry(ancestor.ShaderType, ancestor.Name, ancestor.ExtraDataListLength,
                ancestor.ExtraDataListReferences, ancestor.ControllerObjectReference, ancestor.Flags,
                ancestor.Translation, ancestor.Rotation, ancestor.Scale, ancestor.PropertiesNumber,
                ancestor.PropertiesReferences, ancestor.CollisionObjectReference);
            if (header.Version == 0x14020007 && header.BethesdaVersion >= 100 && ownerObjectName == "NiParticleSystem")
            {
                niGeometry.BoundingSphere = NiBound.Parse(nifReader);
                if (header.BethesdaVersion == 155)
                {
                    niGeometry.BoundMinMax = NIFReaderUtils.ReadFloatArray(nifReader, 6);
                }

                niGeometry.SkinReference = NIFReaderUtils.ReadRef(nifReader);
            }

            if (header.BethesdaVersion < 100)
            {
                niGeometry.DataReference = NIFReaderUtils.ReadRef(nifReader);
                niGeometry.SkinInstanceReference = NIFReaderUtils.ReadRef(nifReader);
                niGeometry.MaterialData = MaterialData.Parse(nifReader, header);
            }
            else if (header.Version == 0x14020007 && header.BethesdaVersion >= 100 &&
                     ownerObjectName != "NiParticleSystem")
            {
                niGeometry.DataReference = NIFReaderUtils.ReadRef(nifReader);
                niGeometry.SkinInstanceReference = NIFReaderUtils.ReadRef(nifReader);
                niGeometry.MaterialData = MaterialData.Parse(nifReader, header);
            }

            if (header.Version != 0x14020007 || header.BethesdaVersion <= 34) return niGeometry;
            niGeometry.ShaderPropertyReference = NIFReaderUtils.ReadRef(nifReader);
            niGeometry.AlphaPropertyReference = NIFReaderUtils.ReadRef(nifReader);


            return niGeometry;
        }
    }
}