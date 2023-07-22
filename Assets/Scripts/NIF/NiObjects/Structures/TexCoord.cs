using System.IO;

namespace NIF.NiObjects.Structures
{
    /// <summary>
    /// Texture coordinates (u,v). As in OpenGL; image origin is in the lower left corner.
    /// </summary>
    public class TexCoord
    {
        public float U { get; private set; }
        
        public float V { get; private set; }
        
        private TexCoord() {}

        public static TexCoord Parse(BinaryReader binaryReader)
        {
            var texCoord = new TexCoord
            {
                U = binaryReader.ReadSingle(),
                V = binaryReader.ReadSingle()
            };
            return texCoord;
        }
    }
}