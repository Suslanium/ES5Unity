using System.IO;

namespace NIF.NiObjects
{
    /// <summary>
    /// A generic extra data object.
    /// </summary>
    public class NiExtraData: NiObject
    {
        public string Name { get; private set; }

        private NiExtraData()
        {
        }

        public NiExtraData(string name)
        {
            Name = name;
        }

        public static NiExtraData Parse(BinaryReader nifReader, string ownerObjectName, Header header)
        {
            var niExtraData = new NiExtraData
            {
                Name = NifReaderUtils.ReadString(nifReader, header)
            };
            return niExtraData;
        }
    }
}