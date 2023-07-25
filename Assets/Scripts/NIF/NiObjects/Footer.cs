using System.IO;

namespace NIF.NiObjects
{
    /// <summary>
    /// The NIF file footer.
    /// </summary>
    public class Footer
    {
        /// <summary>
        /// The number of root references.
        /// </summary>
        public uint RootsNumber { get; private set; }
        
        /// <summary>
        /// List of root NIF objects. If there is a camera, for 1st person view, then this NIF object is referred to as well in this list, even if it is not a root object (usually we want the camera to be attached to the Bip Head node).
        /// </summary>
        public int[] RootReferences { get; private set; }

        private Footer() {}

        public static Footer ParseFooter(BinaryReader nifReader)
        {
            var footer = new Footer
            {
                RootsNumber = nifReader.ReadUInt32()
            };
            footer.RootReferences = NifReaderUtils.ReadRefArray(nifReader, footer.RootsNumber);
            return footer;
        }
    }
}