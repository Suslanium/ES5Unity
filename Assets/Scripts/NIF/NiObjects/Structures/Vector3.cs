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
        
        /// <summary>
        /// Constructor for conversion from UShortVector3 (all values are divided by 1000).
        /// </summary>
        public Vector3(ushort x, ushort y, ushort z)
        {
            X = (float)x/1000;
            Y = (float)y/1000;
            Z = (float)z/1000;
        }

        public static Vector3 Parse(BinaryReader binaryReader)
        {
            return new Vector3
            {
                X = binaryReader.ReadSingle(),
                Y = binaryReader.ReadSingle(),
                Z = binaryReader.ReadSingle()
            };
        }

        public UnityEngine.Vector3 ToUnityVector()
        {
            return new UnityEngine.Vector3(X, Y, Z);
        }
    }
}