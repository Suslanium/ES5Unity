using System.IO;
using NIF.NiObjects.Enums;

namespace NIF.NiObjects
{
    /// <summary>
    /// Determines whether flat shading or smooth shading is used on a shape.
    /// </summary>
    public class NiShadeProperty : NiProperty
    {
        /// <summary>
        /// 0 - SHADING_HARD
        /// 1 - SHADING_SMOOTH
        /// (BS VER LTE FO3)
        /// </summary>
        public ushort ShadeFlags { get; private set; }

        private NiShadeProperty(BsLightingShaderType shaderType, string name, uint extraDataListLength,
            int[] extraDataListReferences, int controllerObjectReference) : base(shaderType, name, extraDataListLength,
            extraDataListReferences, controllerObjectReference)
        {
        }

        public NiShadeProperty(BsLightingShaderType shaderType, string name, uint extraDataListLength,
            int[] extraDataListReferences, int controllerObjectReference, ushort shadeFlags) : base(shaderType, name,
            extraDataListLength, extraDataListReferences, controllerObjectReference)
        {
            ShadeFlags = shadeFlags;
        }

        public new static NiShadeProperty Parse(BinaryReader nifReader, string ownerObjectName, Header header)
        {
            var ancestor = NiProperty.Parse(nifReader, ownerObjectName, header);
            var niShadeProperty = new NiShadeProperty(ancestor.ShaderType, ancestor.Name, ancestor.ExtraDataListLength,
                ancestor.ExtraDataListReferences, ancestor.ControllerObjectReference);
            if (Conditions.NiBsLteFo3(header))
            {
                niShadeProperty.ShadeFlags = nifReader.ReadUInt16();
            }

            return niShadeProperty;
        }
    }
}