using System.IO;

namespace NIF.Parser.NiObjects
{
    /// <summary>
    /// Extra integer data.
    /// </summary>
    public class NiIntegerExtraData : NiExtraData
    {
        /// <summary>
        /// The value of the extra data.
        /// </summary>
        public uint IntegerData { get; private set; }

        private NiIntegerExtraData(string name) : base(name)
        {
        }

        public NiIntegerExtraData(string name, uint integerData) : base(name)
        {
            IntegerData = integerData;
        }

        public new static NiIntegerExtraData Parse(BinaryReader nifReader, string ownerObjectName, Header header)
        {
            var niExtraData = NiExtraData.Parse(nifReader, ownerObjectName, header);
            var niIntegerExtraData = new NiIntegerExtraData(niExtraData.Name)
            {
                IntegerData = nifReader.ReadUInt32()
            };
            return niIntegerExtraData;
        }
    }
}