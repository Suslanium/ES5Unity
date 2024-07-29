using System.IO;

namespace NIF.Parser.NiObjects.Structures
{
    public class BhkQsTransform
    {
        public Vector4 Translation { get; private set; }
        
        public HkQuaternion Rotation { get; private set; }

        private BhkQsTransform()
        {
        }
        
        public static BhkQsTransform Parse(BinaryReader nifReader)
        {
            var qsTransform = new BhkQsTransform
            {
                Translation = Vector4.Parse(nifReader),
                Rotation = HkQuaternion.Parse(nifReader)
            };
            return qsTransform;
        }
    }
}