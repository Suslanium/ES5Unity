using System.IO;

namespace NIF.NiObjects.Structures
{
    /// <summary>
    /// A vector in 3D space (x,y,z).
    /// </summary>
    public class Vector3
    {
        /// <summary>
        /// First coordinate.
        /// </summary>
        public float X { get; private set; }
        
        /// <summary>
        /// Second coordinate.
        /// </summary>
        public float Y { get; private set; }
        
        /// <summary>
        /// Third coordinate.
        /// </summary>
        public float Z { get; private set; }
        
        private Vector3() {}

        public static Vector3 Parse(BinaryReader binaryReader)
        {
            return new Vector3
            {
                X = binaryReader.ReadSingle(),
                Y = binaryReader.ReadSingle(),
                Z = binaryReader.ReadSingle()
            };
        }
    }
}