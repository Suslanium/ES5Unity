using System.IO;

namespace NIF.NiObjects.Structures
{
    /// <summary>
    /// A 4-dimensional vector.
    /// </summary>
    public class Vector4
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
        
        /// <summary>
        /// Fourth coordinate.
        /// </summary>
        public float W { get; private set; }
        
        private Vector4() {}

        public static Vector4 Parse(BinaryReader binaryReader)
        {
            return new Vector4
            {
                X = binaryReader.ReadSingle(),
                Y = binaryReader.ReadSingle(),
                Z = binaryReader.ReadSingle(),
                W = binaryReader.ReadSingle()
            };
        }

        public UnityEngine.Vector4 ToUnityVector()
        {
            return new UnityEngine.Vector4(X, Y, Z, W);
        }
    }
}