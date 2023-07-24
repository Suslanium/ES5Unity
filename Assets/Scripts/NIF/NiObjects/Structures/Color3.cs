using System.IO;

namespace NIF.NiObjects.Structures
{
    public class Color3
    {
        public float R { get; private set; }
        
            public float G { get; private set; }
        
            public float B { get; private set; }

            private Color3() {}

            public static Color3 Parse(BinaryReader binaryReader)
            {
                var color = new Color3
                {
                    R = binaryReader.ReadSingle(),
                    G = binaryReader.ReadSingle(),
                    B = binaryReader.ReadSingle(),
                };
                return color;
            }
    }
}