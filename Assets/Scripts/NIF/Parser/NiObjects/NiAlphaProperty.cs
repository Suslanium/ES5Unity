using System.IO;
using NIF.Parser.NiObjects.Enums;
using NIF.Parser.NiObjects.Structures;

namespace NIF.Parser.NiObjects
{
    /// <summary>
    /// Transparency
    /// </summary>
    public class NiAlphaProperty : NiProperty
    {
        public AlphaFlags AlphaFlags { get; private set; }

        public byte Threshold { get; private set; }

        private NiAlphaProperty(BsLightingShaderType shaderType, string name, uint extraDataListLength,
            int[] extraDataListReferences, int controllerObjectReference) : base(shaderType, name, extraDataListLength,
            extraDataListReferences, controllerObjectReference)
        {
        }
        
        public new static NiAlphaProperty Parse(BinaryReader nifReader, string ownerObjectName, Header header)
        {
            var ancestor = NiProperty.Parse(nifReader, ownerObjectName, header);
            var alphaProperty = new NiAlphaProperty(ancestor.ShaderType, ancestor.Name, ancestor.ExtraDataListLength,
                ancestor.ExtraDataListReferences, ancestor.ControllerObjectReference)
            {
                AlphaFlags = AlphaFlags.Parse(nifReader),
                Threshold = nifReader.ReadByte()
            };
            return alphaProperty;
        }
    }
}