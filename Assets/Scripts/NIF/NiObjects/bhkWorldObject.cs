using System.IO;
using NIF.NiObjects.Enums;

namespace NIF.NiObjects
{
    /// <summary>
    /// Bethesda extension of hkpWorldObject, the base class for hkpEntity and hkpPhantom.
    /// </summary>
    public class BhkWorldObject : BhkSerializable
    {
        public int ShapeReference { get; private set; }

        public SkyrimLayer Layer { get; private set; }

        public byte CollisionFilterFlags { get; private set; }

        public ushort Group { get; private set; }

        private BhkWorldObject()
        {
        }

        public static BhkWorldObject Parse(BinaryReader nifReader, string ownerObjectName, Header header)
        {
            var bhkWorldObject = new BhkWorldObject
            {
                ShapeReference = NifReaderUtils.ReadRef(nifReader)
            };
            if (header.Version < 0x0A000102) nifReader.BaseStream.Seek(4, SeekOrigin.Current);
            if (header.Version == 0x14000007 && Conditions.BsGtFo3(header))
            {
                var layerIndex = nifReader.ReadByte();
                bhkWorldObject.Layer = (SkyrimLayer)layerIndex;
            }
            else
            {
                nifReader.BaseStream.Seek(1, SeekOrigin.Current);
            }

            bhkWorldObject.CollisionFilterFlags = nifReader.ReadByte();
            bhkWorldObject.Group = nifReader.ReadUInt16();
            //Skipping bhkWorldObjectCInfo
            nifReader.BaseStream.Seek(20, SeekOrigin.Current);
            return bhkWorldObject;
        }
    }
}