using System.IO;
using NIF.Parser.NiObjects.Enums;

namespace NIF.Parser.NiObjects
{
    /// <summary>
    /// Bethesda-specific property.
    /// (All fields specific to this class are only present in Fallout 3 NIFs)
    /// </summary>
    public class BsShaderProperty : NiShadeProperty
    {
        public uint Fo3ShaderType { get; private set; }

        public uint Fo3ShaderFlags { get; private set; }

        public uint Fo3ShaderFlags2 { get; private set; }

        /// <summary>
        /// Scales the intensity of the environment/cube map.
        /// </summary>
        public float Fo3EnvMapScale { get; private set; }

        private BsShaderProperty(BsLightingShaderType shaderType, string name, uint extraDataListLength,
            int[] extraDataListReferences, int controllerObjectReference, ushort shadeFlags) : base(shaderType, name,
            extraDataListLength, extraDataListReferences, controllerObjectReference, shadeFlags)
        {
        }

        public BsShaderProperty(BsLightingShaderType shaderType, string name, uint extraDataListLength,
            int[] extraDataListReferences, int controllerObjectReference, ushort shadeFlags, uint fo3ShaderType,
            uint fo3ShaderFlags, uint fo3ShaderFlags2, float fo3EnvMapScale) : base(shaderType, name,
            extraDataListLength, extraDataListReferences, controllerObjectReference, shadeFlags)
        {
            Fo3ShaderType = fo3ShaderType;
            Fo3ShaderFlags = fo3ShaderFlags;
            Fo3ShaderFlags2 = fo3ShaderFlags2;
            Fo3EnvMapScale = fo3EnvMapScale;
        }

        public new static BsShaderProperty Parse(BinaryReader nifReader, string ownerObjectName, Header header)
        {
            var ancestor = NiShadeProperty.Parse(nifReader, ownerObjectName, header);
            var bsShaderProperty = new BsShaderProperty(ancestor.ShaderType, ancestor.Name,
                ancestor.ExtraDataListLength,
                ancestor.ExtraDataListReferences, ancestor.ControllerObjectReference, ancestor.ShadeFlags);
            if (Conditions.BsGtFo3(header)) return bsShaderProperty;
            bsShaderProperty.Fo3ShaderType = nifReader.ReadUInt32();
            bsShaderProperty.Fo3ShaderFlags = nifReader.ReadUInt32();
            bsShaderProperty.Fo3ShaderFlags2 = nifReader.ReadUInt32();
            bsShaderProperty.Fo3EnvMapScale = nifReader.ReadSingle();

            return bsShaderProperty;
        }
    }
}