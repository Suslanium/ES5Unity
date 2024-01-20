using System.IO;

namespace NIF.NiObjects.Structures
{
    /// <summary>
    /// A quaternion as it appears in the havok objects.
    /// </summary>
    public class HkQuaternion
    {
        public float X { get; private set; }

        public float Y { get; private set; }
        
        public float Z { get; private set; }
        
        public float W { get; private set; }
        
        private HkQuaternion() {}

        public static HkQuaternion Parse(BinaryReader binaryReader)
        {
            return new HkQuaternion()
            {
                X = binaryReader.ReadSingle(),
                Y = binaryReader.ReadSingle(),
                Z = binaryReader.ReadSingle(),
                W = binaryReader.ReadSingle()
            };
        }

        public UnityEngine.Quaternion ToUnityVector()
        {
            return new UnityEngine.Quaternion(X, Y, Z, W);
        }
    }
}