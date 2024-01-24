using UnityEngine;

namespace MasterFile.MasterFileContents.Records.Structures
{
    /// <summary>
    /// 32 byte struct
    /// </summary>
    public class Primitive
    {
        public Vector3 Bounds { get; private set; }
        
        public Color Color { get; private set; }
        
        /// <summary>
        /// float - unknown: 0.15, 0.2, 0.25, 1.0 seen; same for any given base object
        /// </summary>
        public float Unknown { get; private set; }
        
        /// <summary>
        /// uint32 - unknown: 1-4 seen
        /// 1 - Box
        /// 2 - Sphere
        /// 3 - Portal Box
        /// 4 - Unknown
        /// </summary>
        public uint Unknown2 { get; private set; }

        public Primitive(Vector3 bounds, Color color, float unknown, uint unknown2)
        {
            Bounds = bounds;
            Color = color;
            Unknown = unknown;
            Unknown2 = unknown2;
        }
    }
}