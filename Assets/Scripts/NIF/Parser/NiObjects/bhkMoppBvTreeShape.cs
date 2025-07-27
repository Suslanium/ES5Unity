using System.IO;
using NIF.Parser.NiObjects.Enums;
using NIF.Parser.NiObjects.Structures;

namespace NIF.Parser.NiObjects
{
    /// <summary>
    /// Bethesda extension of hkpMoppBvTreeShape. hkpMoppBvTreeShape is a bounding volume tree using Havok-proprietary MOPP code.
    /// </summary>
    public class BhkMoppBvTreeShape : BhkBvTreeShape
    {
        public float Scale { get; private set; }
        
        /// <summary>
        /// Number of bytes for MOPP data.
        /// </summary>
        public uint DataSize { get; private set; }
        
        /// <summary>
        /// XYZ: Origin of the object in mopp coordinates. This is the minimum of all vertices in the packed shape along each axis, minus the radius of the child bhkPackedNiTriStripsShape/bhkCompressedMeshShape.
        /// W: The scaling factor to quantize the MOPP: the quantization factor is equal to 256*256 divided by this number.In Oblivion and Skyrim files, scale is taken equal to 256*256*254 / (size + 2 * radius) where size is the largest dimension of the bounding box of the packed shape, and radius is the radius of the child bhkPackedNiTriStripsShape/bhkCompressedMeshShape.
        /// </summary>
        public Vector4 Offset { get; private set; }
         
        /// <summary>
        /// Tells if MOPP Data was organized into smaller chunks (PS3) or not (PC)
        /// </summary>
        public HkMoppCodeBuildType BuildType { get; private set; }
        
        /// <summary>
        /// The tree of bounding volume data.
        /// https://github.com/niftools/nifxml/wiki/Havok-MOPP-Data-format
        /// </summary>
        public byte[] Data { get; private set; }
        
        private BhkMoppBvTreeShape(int shapeReference) : base(shapeReference)
        {
        }

        public new static BhkMoppBvTreeShape Parse(BinaryReader nifReader, string ownerObjectName, Header header)
        {
            var ancestor = BhkBvTreeShape.Parse(nifReader, ownerObjectName, header);
            var bhkMoppBvTreeShape = new BhkMoppBvTreeShape(ancestor.ShapeReference);
            nifReader.BaseStream.Seek(12, SeekOrigin.Current);
            bhkMoppBvTreeShape.Scale = nifReader.ReadSingle();
            bhkMoppBvTreeShape.DataSize = nifReader.ReadUInt32();
            if (header.Version >= 0x0A010000)
            {
                bhkMoppBvTreeShape.Offset = Vector4.Parse(nifReader);
            }

            if (Conditions.BsGtFo3(header))
            {
                bhkMoppBvTreeShape.BuildType = (HkMoppCodeBuildType)nifReader.ReadByte();
            }

            bhkMoppBvTreeShape.Data = nifReader.ReadBytes((int) bhkMoppBvTreeShape.DataSize);
            return bhkMoppBvTreeShape;
        }
    }
}