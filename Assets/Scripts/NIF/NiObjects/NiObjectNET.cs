using System.IO;
using NIF.NiObjects.Enums;

namespace NIF.NiObjects
{
    /// <summary>
    /// Abstract(well, not really abstract in this implementation) base class for NiObjects that support names, extra data, and time controllers.
    /// </summary>
    public class NiObjectNET : NiObject
    {
        /// <summary>
        /// Configures the main shader path
        /// </summary>
        public BSLightingShaderType ShaderType { get; private set; }

        /// <summary>
        /// Name of this controllable object, used to refer to the object in .kf files.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The number of Extra Data objects referenced through the list.
        /// </summary>
        public uint ExtraDataListLength { get; private set; }

        /// <summary>
        /// List of extra data indices.
        /// </summary>
        public int[] ExtraDataListReferences { get; private set; }

        /// <summary>
        /// Controller object index. (The first in a chain)
        /// </summary>
        public int ControllerObjectReference { get; private set; }

        private NiObjectNET()
        {
        }

        protected NiObjectNET(BSLightingShaderType shaderType, string name, uint extraDataListLength,
            int[] extraDataListReferences, int controllerObjectReference)
        {
            ShaderType = shaderType;
            Name = name;
            ExtraDataListLength = extraDataListLength;
            ExtraDataListReferences = extraDataListReferences;
            ControllerObjectReference = controllerObjectReference;
        }

        protected static NiObjectNET Parse(BinaryReader nifReader, string ownerObjectName, Header header)
        {
            var niObjectNet = new NiObjectNET();
            if (ownerObjectName == "BSLightingShaderProperty" && header.Version == 0x14020007 &&
                header.BethesdaVersion is >= 83 and <= 139)
            {
                niObjectNet.ShaderType = (BSLightingShaderType)checked((int)nifReader.ReadUInt32());
            }

            niObjectNet.Name = NIFReaderUtils.ReadString(nifReader, header);
            niObjectNet.ExtraDataListLength = nifReader.ReadUInt32();
            niObjectNet.ExtraDataListReferences =
                NIFReaderUtils.ReadRefArray(nifReader, niObjectNet.ExtraDataListLength);
            niObjectNet.ControllerObjectReference = NIFReaderUtils.ReadRef(nifReader);

            return niObjectNet;
        }
    }
}