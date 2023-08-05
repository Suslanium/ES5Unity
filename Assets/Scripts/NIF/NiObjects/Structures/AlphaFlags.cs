using System.IO;
using UnityEngine.Rendering;

namespace NIF.NiObjects.Structures
{
    public class AlphaFlags
    {
        public bool AlphaBlend { get; private set; }
        
        public BlendMode SourceBlendMode { get; private set; }
        
        public BlendMode DestinationBlendMode { get; private set; }
        
        public bool AlphaTest { get; private set; }

        private AlphaFlags() {}
        
        public static AlphaFlags Parse(BinaryReader binaryReader)
        {
            var alphaFlagsVal = binaryReader.ReadUInt16();
            var alphaFlags = new AlphaFlags();
            if ((alphaFlagsVal & 0x0001) != 0) alphaFlags.AlphaBlend = true;
            var srcBlendMode = alphaFlagsVal & 0x001E;
            alphaFlags.SourceBlendMode = ParseAlphaFunction(srcBlendMode, BlendMode.SrcAlpha);
            var destBlendMode = alphaFlagsVal & 0x01E0;
            alphaFlags.DestinationBlendMode = ParseAlphaFunction(destBlendMode, BlendMode.OneMinusSrcAlpha);
            if ((alphaFlagsVal & 0x0200) != 0) alphaFlags.AlphaTest = true;
            return alphaFlags;
        }

        private static BlendMode ParseAlphaFunction(int srcBlendMode, BlendMode defaultMode)
        {
            switch (srcBlendMode)
            {
                case 0:
                    return BlendMode.One;
                case 1:
                    return BlendMode.Zero;
                case 2:
                    return BlendMode.SrcColor;
                case 3:
                    return BlendMode.OneMinusSrcColor;
                case 4:
                    return BlendMode.DstColor;
                case 5:
                    return BlendMode.OneMinusDstColor;
                case 6:
                    return BlendMode.SrcAlpha;
                case 7:
                    return BlendMode.OneMinusSrcAlpha;
                case 8:
                    return BlendMode.DstAlpha;
                case 9:
                    return BlendMode.OneMinusDstAlpha;
                case 10:
                    return BlendMode.SrcAlphaSaturate;
            }

            return defaultMode;
        }
    }
}