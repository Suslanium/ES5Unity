using System.IO;
using NIF.NiObjects.Enums;

namespace NIF.NiObjects
{
    /// <summary>
    /// Bethesda-specific property.
    /// (All fields specific to this class are only present in Fallout 3 NIFs)
    /// </summary>
    public class BSShaderProperty : NiShadeProperty
    {
        public uint FO3ShaderType { get; private set; }

        public uint FO3ShaderFlags { get; private set; }

        public uint FO3ShaderFlags2 { get; private set; }

        /// <summary>
        /// Scales the intensity of the environment/cube map.
        /// </summary>
        public float FO3EnvMapScale { get; private set; }

        private BSShaderProperty(BSLightingShaderType shaderType, string name, uint extraDataListLength,
            int[] extraDataListReferences, int controllerObjectReference, ushort shadeFlags) : base(shaderType, name,
            extraDataListLength, extraDataListReferences, controllerObjectReference, shadeFlags)
        {
        }

        public BSShaderProperty(BSLightingShaderType shaderType, string name, uint extraDataListLength,
            int[] extraDataListReferences, int controllerObjectReference, ushort shadeFlags, uint fo3ShaderType,
            uint fo3ShaderFlags, uint fo3ShaderFlags2, float fo3EnvMapScale) : base(shaderType, name,
            extraDataListLength, extraDataListReferences, controllerObjectReference, shadeFlags)
        {
            FO3ShaderType = fo3ShaderType;
            FO3ShaderFlags = fo3ShaderFlags;
            FO3ShaderFlags2 = fo3ShaderFlags2;
            FO3EnvMapScale = fo3EnvMapScale;
        }

        public new static BSShaderProperty Parse(BinaryReader nifReader, string ownerObjectName, Header header)
        {
            var ancestor = NiShadeProperty.Parse(nifReader, ownerObjectName, header);
            var bsShaderProperty = new BSShaderProperty(ancestor.ShaderType, ancestor.Name,
                ancestor.ExtraDataListLength,
                ancestor.ExtraDataListReferences, ancestor.ControllerObjectReference, ancestor.ShadeFlags);
            if (header.BethesdaVersion > 34) return bsShaderProperty;
            bsShaderProperty.FO3ShaderType = nifReader.ReadUInt32();
            bsShaderProperty.FO3ShaderFlags = nifReader.ReadUInt32();
            bsShaderProperty.FO3ShaderFlags2 = nifReader.ReadUInt32();
            bsShaderProperty.FO3EnvMapScale = nifReader.ReadSingle();

            return bsShaderProperty;
        }
    }
}