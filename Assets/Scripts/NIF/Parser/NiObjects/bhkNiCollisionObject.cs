using System.IO;

namespace NIF.Parser.NiObjects
{
    /// <summary>
    /// Abstract base class to merge NiCollisionObject with Bethesda Havok.
    /// </summary>
    public class BhkNiCollisionObject : NiCollisionObject
    {
        /// <summary>
        /// bhkNiCollisionObject flags.
        /// </summary>
        public ushort BhkCoFlags { get; private set; }

        public int BodyReference { get; private set; }

        private BhkNiCollisionObject(int target) : base(target)
        {
        }

        protected BhkNiCollisionObject(int target, ushort bhkCoFlags, int bodyReference) : base(target)
        {
            BhkCoFlags = bhkCoFlags;
            BodyReference = bodyReference;
        }

        protected new static BhkNiCollisionObject Parse(BinaryReader nifReader, string ownerObjectName, Header header)
        {
            var ancestor = NiCollisionObject.Parse(nifReader, ownerObjectName, header);
            var bhkNiCollisionObject = new BhkNiCollisionObject(ancestor.Target)
            {
                BhkCoFlags = nifReader.ReadUInt16(),
                BodyReference = NifReaderUtils.ReadRef(nifReader)
            };
            return bhkNiCollisionObject;
        }
    }
}