using System.IO;
using NIF.NiObjects.Structures;

namespace NIF.NiObjects
{
    /// <summary>
    /// Compressed collision mesh.
    /// </summary>
    public class BhkCompressedMeshShape: BhkShape
    {
        /// <summary>
        /// Points to root node?
        /// </summary>
        public int Target { get; private set; }
        
        public uint UserData { get; private set; }
        
        /// <summary>
        /// A shell that is added around the shape.
        /// </summary>
        public float Radius { get; private set; }
        
        public Vector4 Scale { get; private set; }
        
        public float RadiusCopy { get; private set; }
        
        public Vector4 ScaleCopy { get; private set; }
        
        /// <summary>
        /// The collision mesh data.
        /// </summary>
        public int DataRef { get; private set; }

        private BhkCompressedMeshShape()
        {
        }

        public static BhkCompressedMeshShape Parse(BinaryReader nifReader, string ownerObjectName, Header header)
        {
            var bhkCompressedMeshShape = new BhkCompressedMeshShape
            {
                Target = NifReaderUtils.ReadRef(nifReader),
                UserData = nifReader.ReadUInt32(),
                Radius = nifReader.ReadSingle()
            };
            nifReader.BaseStream.Seek(4, SeekOrigin.Current);
            bhkCompressedMeshShape.Scale = Vector4.Parse(nifReader);
            bhkCompressedMeshShape.RadiusCopy = nifReader.ReadSingle();
            bhkCompressedMeshShape.ScaleCopy = Vector4.Parse(nifReader);
            bhkCompressedMeshShape.DataRef = NifReaderUtils.ReadRef(nifReader);
            return bhkCompressedMeshShape;
        }
    }
}