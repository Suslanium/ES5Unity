using System.IO;
using NIF.NiObjects.Enums;

namespace NIF.NiObjects
{
    /// <summary>
    /// Abstract base class representing all rendering properties. Subclasses are attached to NiAVObjects to control their rendering.
    /// </summary>
    public class NiProperty : NiObjectNET
    {
        protected NiProperty(BSLightingShaderType shaderType, string name, uint extraDataListLength,
            int[] extraDataListReferences, int controllerObjectReference) : base(shaderType, name, extraDataListLength,
            extraDataListReferences, controllerObjectReference)
        {
        }

        protected new static NiProperty Parse(BinaryReader nifReader, string ownerObjectName, Header header)
        {
            var ancestor = NiObjectNET.Parse(nifReader, ownerObjectName, header);
            return new NiProperty(ancestor.ShaderType, ancestor.Name, ancestor.ExtraDataListLength,
                ancestor.ExtraDataListReferences, ancestor.ControllerObjectReference);
        }
    }
}