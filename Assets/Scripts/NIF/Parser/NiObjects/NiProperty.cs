using System.IO;
using NIF.Parser.NiObjects.Enums;

namespace NIF.Parser.NiObjects
{
    /// <summary>
    /// Abstract base class representing all rendering properties. Subclasses are attached to NiAVObjects to control their rendering.
    /// </summary>
    public class NiProperty : NiObjectNet
    {
        protected NiProperty(BsLightingShaderType shaderType, string name, uint extraDataListLength,
            int[] extraDataListReferences, int controllerObjectReference) : base(shaderType, name, extraDataListLength,
            extraDataListReferences, controllerObjectReference)
        {
        }

        protected new static NiProperty Parse(BinaryReader nifReader, string ownerObjectName, Header header)
        {
            var ancestor = NiObjectNet.Parse(nifReader, ownerObjectName, header);
            return new NiProperty(ancestor.ShaderType, ancestor.Name, ancestor.ExtraDataListLength,
                ancestor.ExtraDataListReferences, ancestor.ControllerObjectReference);
        }
    }
}