using System.IO;

namespace NIF.Parser.NiObjects.Structures
{
    public class MaterialData
    {
        public bool HasShader { get; private set; }

        public string ShaderName { get; private set; }
        
        /// <summary>
        /// Extra data associated with the shader. A value of -1 means the shader is the default implementation.
        /// </summary>
        public int ShaderExtraData { get; private set; }
        
        public uint NumberOfMaterials { get; private set; }

        public string[] MaterialNames { get; private set; }

        /// <summary>
        /// Extra data associated with the material. A value of -1 means the material is the default implementation.
        /// </summary>
        public int[] MaterialExtraData { get; private set; }

        public int ActiveMaterialIndex { get; private set; }

        /// <summary>
        /// Whether the materials for this object always needs to be updated before rendering with them.
        /// </summary>
        public bool MaterialNeedsUpdate { get; private set; }

        private MaterialData()
        {
        }

        public static MaterialData Parse(BinaryReader binaryReader, Header header)
        {
            var matData = new MaterialData();
            if (header.Version is >= 0x0A000100 and <= 0x14010003)
            {
                matData.HasShader = binaryReader.ReadBoolean();
                if (matData.HasShader)
                {
                    matData.ShaderName = NifReaderUtils.ReadString(binaryReader, header);
                    matData.ShaderExtraData = binaryReader.ReadInt32();
                }
            }

            if (header.Version >= 0x14020005)
            {
                matData.NumberOfMaterials = binaryReader.ReadUInt32();
                matData.MaterialNames = NifReaderUtils.ReadStringArray(binaryReader, header, matData.NumberOfMaterials);
                matData.MaterialExtraData = NifReaderUtils.ReadRefArray(binaryReader, matData.NumberOfMaterials);
                matData.ActiveMaterialIndex = binaryReader.ReadInt32();
            }

            if (header.Version >= 0x14020007)
            {
                matData.MaterialNeedsUpdate = binaryReader.ReadBoolean();
            }

            return matData;
        }
    }
}