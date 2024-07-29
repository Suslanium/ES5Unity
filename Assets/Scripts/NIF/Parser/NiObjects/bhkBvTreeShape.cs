using System.IO;

namespace NIF.Parser.NiObjects
{
    /// <summary>
    /// Bethesda extension of hkpBvTreeShape. hkpBvTreeShape adds a bounding volume tree to an hkpShapeCollection. A bounding volume tree is useful for testing collision between a moving object and large static geometry.
    /// </summary>
    public class BhkBvTreeShape: BhkShape
    {
        public int ShapeReference { get; private set; }

        private BhkBvTreeShape()
        {
        }

        protected BhkBvTreeShape(int shapeReference)
        {
            ShapeReference = shapeReference;
        }

        public static BhkBvTreeShape Parse(BinaryReader nifReader, string ownerObjectName, Header header)
        {
            return new BhkBvTreeShape
            {
                ShapeReference = NifReaderUtils.ReadRef(nifReader)
            };
        }
    }
}