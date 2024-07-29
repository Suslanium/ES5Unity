using System.IO;
using NIF.Parser.NiObjects.Enums;

namespace NIF.Parser.NiObjects
{
    /// <summary>
    /// An interface that produces a set of spheres that represent a simplified version of the shape.
    /// </summary>
    public class BhkSphereRepShape: BhkConvexShapeBase
    {
        public SkyrimHavokMaterial Material { get; private set; }

        private BhkSphereRepShape()
        {
        }

        protected BhkSphereRepShape(SkyrimHavokMaterial material)
        {
            Material = material;
        }
        
        public static BhkSphereRepShape Parse(BinaryReader nifReader, string ownerObjectName, Header header)
        {
            var bhkSphereRepShape = new BhkSphereRepShape();
            if (header.Version < 0x0A000102)
            {
                nifReader.BaseStream.Seek(4, SeekOrigin.Current);
            }
            if (header.Version == 0x14020007 && Conditions.BsGtFo3(header))
            {
                bhkSphereRepShape.Material = (SkyrimHavokMaterial)nifReader.ReadUInt32();
            }
            else
            {
                nifReader.BaseStream.Seek(4, SeekOrigin.Current);
            }
            return bhkSphereRepShape;
        }
    }
}