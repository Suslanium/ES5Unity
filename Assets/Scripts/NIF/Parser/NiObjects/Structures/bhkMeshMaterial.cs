using System.IO;
using NIF.Parser.NiObjects.Enums;

namespace NIF.Parser.NiObjects.Structures
{
    public class BhkMeshMaterial
    {
        public SkyrimHavokMaterial Material { get; private set; }
        
        public SkyrimLayer SkyrimLayer { get; private set; }
        
        public byte CollisionFilterFlags { get; private set; }
        
        public ushort Group { get; private set; }

        private BhkMeshMaterial()
        {
        }
        
        public static BhkMeshMaterial Parse(BinaryReader nifReader, string ownerObjectName, Header header)
        {
            var meshMat = new BhkMeshMaterial
            {
                Material = (SkyrimHavokMaterial) nifReader.ReadUInt32(),
            };
            if (header.Version == 0x14000007 && Conditions.BsGtFo3(header))
            {
                meshMat.SkyrimLayer = (SkyrimLayer)nifReader.ReadByte();
            }
            else
            {
                nifReader.BaseStream.Seek(1, SeekOrigin.Current);
            }
            meshMat.CollisionFilterFlags = nifReader.ReadByte();
            meshMat.Group = nifReader.ReadUInt16();
            return meshMat;
        }
    }
}