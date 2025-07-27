using System.IO;
using NIF.Parser.NiObjects.Enums;

namespace NIF.Parser.NiObjects
{
    /// <summary>
    /// Abstract(well, not really abstract in this implementation) base class for NiObjects that support names, extra data, and time controllers.
    /// </summary>
    public class NiObjectNet : NiObject
    {
        /// <summary>
        /// Configures the main shader path
        /// </summary>
        public BsLightingShaderType ShaderType { get; private set; }

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

        private NiObjectNet()
        {
        }

        protected NiObjectNet(BsLightingShaderType shaderType, string name, uint extraDataListLength,
            int[] extraDataListReferences, int controllerObjectReference)
        {
            ShaderType = shaderType;
            Name = name;
            ExtraDataListLength = extraDataListLength;
            ExtraDataListReferences = extraDataListReferences;
            ControllerObjectReference = controllerObjectReference;
        }

        protected static NiObjectNet Parse(BinaryReader nifReader, string ownerObjectName, Header header)
        {
            var niObjectNet = new NiObjectNet();
            if (ownerObjectName == "BSLightingShaderProperty" && header.Version == 0x14020007 &&
                Conditions.BsGteSky(header) && Conditions.NiBsLteFo4(header))
            {
                niObjectNet.ShaderType = (BsLightingShaderType)checked((int)nifReader.ReadUInt32());
            }

            niObjectNet.Name = NifReaderUtils.ReadString(nifReader, header);
            niObjectNet.ExtraDataListLength = nifReader.ReadUInt32();
            niObjectNet.ExtraDataListReferences =
                NifReaderUtils.ReadRefArray(nifReader, niObjectNet.ExtraDataListLength);
            niObjectNet.ControllerObjectReference = NifReaderUtils.ReadRef(nifReader);

            return niObjectNet;
        }
    }
}