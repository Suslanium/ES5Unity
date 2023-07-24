using System.IO;

namespace NIF.NiObjects
{
    /// <summary>
    /// Bethesda-specific Texture Set.
    /// </summary>
    public class BSShaderTextureSet: NiObject
    {
        public uint NumberOfTextures { get; private set; }
        /// <summary>
        /// <para>0: Diffuse</para>
        /// <para>1: Normal/Gloss</para>
        /// <para>2: Glow(SLSF2_Glow_Map)/Skin/Hair/Rim light(SLSF2_Rim_Lighting)</para>
        /// <para>3: Height/Parallax</para>
        /// <para>4: Environment</para>
        /// <para>5: Environment Mask</para>
        /// <para>6: Subsurface for Multilayer Parallax</para>
        /// <para>7: Back Lighting Map (SLSF2_Back_Lighting)</para>
        /// </summary>
        public string[] Textures { get; private set; }

        private BSShaderTextureSet()
        {
        }

        public static BSShaderTextureSet Parse(BinaryReader nifReader, string ownerObjectName, Header header)
        {
            var textureSet = new BSShaderTextureSet
            {
                NumberOfTextures = nifReader.ReadUInt32()
            };
            textureSet.Textures = NIFReaderUtils.ReadSizedStringArray(nifReader, textureSet.NumberOfTextures);
            return textureSet;
        }
    }
}