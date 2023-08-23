using System.IO;

namespace NIF.NiObjects
{
    /// <summary>
    /// Primary Bethesda Havok object.
    /// </summary>
    public class BhkCollisionObject : BhkNiCollisionObject
    {
        private BhkCollisionObject(int target, ushort bhkCoFlags, int bodyReference) : base(target, bhkCoFlags,
            bodyReference)
        {
        }

        public new static BhkCollisionObject Parse(BinaryReader nifReader, string ownerObjectName, Header header)
        {
            var ancestor = BhkNiCollisionObject.Parse(nifReader, ownerObjectName, header);
            return new BhkCollisionObject(ancestor.Target, ancestor.BhkCoFlags, ancestor.BodyReference);
        }
    }
}