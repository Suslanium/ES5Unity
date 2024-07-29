using System.IO;
using UnityEngine;

namespace NIF.Parser.NiObjects.Structures
{
    /// <summary>
    /// A color with alpha (red, green, blue, alpha).
    /// </summary>
    public class Color4
    {
        public float R { get; private set; }
        
        public float G { get; private set; }
        
        public float B { get; private set; }
        
        public float A { get; private set; }

        private Color4() {}

        public static Color4 Parse(BinaryReader binaryReader)
        {
            var color = new Color4
            {
                R = binaryReader.ReadSingle(),
                G = binaryReader.ReadSingle(),
                B = binaryReader.ReadSingle(),
                A = binaryReader.ReadSingle()
            };
            return color;
        }

        public Color ToColor()
        {
            return new Color(R, G, B, A);
        }
    }
}