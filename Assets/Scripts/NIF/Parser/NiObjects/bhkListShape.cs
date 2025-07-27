using System.IO;
using NIF.Parser.NiObjects.Enums;

namespace NIF.Parser.NiObjects
{
    public class BhkListShape: BhkShapeCollection
    {
        public uint NumSubShapes { get; private set; }
        
        public int[] SubShapeReferences { get; private set; }
        
        public SkyrimHavokMaterial Material { get; private set; }

        private BhkListShape()
        {
        }

        public static BhkListShape Parse(BinaryReader nifReader, string ownerObjectName, Header header)
        {
            var bhkListShape = new BhkListShape
            {
                NumSubShapes = nifReader.ReadUInt32()
            };
            bhkListShape.SubShapeReferences = NifReaderUtils.ReadRefArray(nifReader, bhkListShape.NumSubShapes);
            if (header.Version < 0x0A000102)
            {
                nifReader.BaseStream.Seek(4, SeekOrigin.Current);
            }
            if (header.Version == 0x14020007 && Conditions.BsGtFo3(header))
            {
                bhkListShape.Material = (SkyrimHavokMaterial)nifReader.ReadUInt32();
            }
            else
            {
                nifReader.BaseStream.Seek(4, SeekOrigin.Current);
            }
            nifReader.BaseStream.Seek(24, SeekOrigin.Current);
            var numFilters = nifReader.ReadUInt32();
            nifReader.BaseStream.Seek(numFilters * 4, SeekOrigin.Current);
            return bhkListShape;
        }
    }
}