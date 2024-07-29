using System.IO;
using NIF.Parser.NiObjects.Enums;

namespace NIF.Parser.NiObjects
{
    /// <summary>
    /// An interface that allows testing convex sets using the GJK algorithm. Also holds a radius value for creating a shell.
    /// </summary>
    public class BhkConvexShape: BhkSphereRepShape
    {
        /// <summary>
        /// The radius is used to create a thin shell that is used as the shape surface.
        /// </summary>
        public float Radius { get; private set; }
        
        private BhkConvexShape(SkyrimHavokMaterial material) : base(material)
        {
        }

        protected BhkConvexShape(SkyrimHavokMaterial material, float radius) : base(material)
        {
            Radius = radius;
        }

        public new static BhkConvexShape Parse(BinaryReader binaryReader, string ownerObjectName, Header header)
        {
            var ancestor = BhkSphereRepShape.Parse(binaryReader, ownerObjectName, header);
            var bhkConvexShape = new BhkConvexShape(ancestor.Material)
            {
                Radius = binaryReader.ReadSingle()
            };
            return bhkConvexShape;
        }
    }
}