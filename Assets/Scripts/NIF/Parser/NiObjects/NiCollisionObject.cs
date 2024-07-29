using System.IO;

namespace NIF.Parser.NiObjects
{
    /// <summary>
    /// This is the most common collision object found in NIF files. It acts as a real object that
    /// is visible and possibly (if the body allows for it) interactive. The node itself
    /// is simple, it only has three properties.
    /// For this type of collision object, bhkRigidBody or bhkRigidBodyT is generally used.
    /// </summary>
    public class NiCollisionObject : NiObject
    {
        /// <summary>
        /// Index of the AV object referring to this collision object.
        /// </summary>
        public int Target { get; private set; }

        private NiCollisionObject()
        {
        }

        protected NiCollisionObject(int target)
        {
            Target = target;
        }

        public static NiCollisionObject Parse(BinaryReader nifReader, string ownerObjectName, Header header)
        {
            var niCollisionObject = new NiCollisionObject
            {
                Target = NifReaderUtils.ReadRef(nifReader)
            };
            return niCollisionObject;
        }
    }
}