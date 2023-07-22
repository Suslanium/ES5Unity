using System.IO;

namespace NIF.NiObjects.Structures
{
    /// <summary>
    /// A sphere.
    /// </summary>
    public class NiBound
    {
        /// <summary>
        /// The sphere's center.
        /// </summary>
        public Vector3 Center { get; private set; }
        
        /// <summary>
        /// The sphere's radius.
        /// </summary>
        public float Radius { get; private set; }
        
        private NiBound() {}

        public static NiBound Parse(BinaryReader binaryReader)
        {
            var niBound = new NiBound
            {
                Center = Vector3.Parse(binaryReader),
                Radius = binaryReader.ReadSingle()
            };
            return niBound;
        }
    }
}