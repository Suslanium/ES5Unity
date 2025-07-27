using System.Collections.Generic;
using System.IO;
using NIF.Parser.NiObjects;
using Logger = Engine.Core.Logger;

namespace NIF.Parser
{
    public delegate NiObject ParseFunction(BinaryReader nifReader, string ownerObjectName, Header header);

    //TODO replace this with DI or something
    public static class NiObjectParsers
    {
        public static readonly Dictionary<string, ParseFunction> ParseFunctions = new()
        {
            { "NiNode", NiNode.Parse },
            { "BSFadeNode", BsFadeNode.Parse },
            { "BSLeafAnimNode", BsLeafAnimNode.Parse },
            { "BSTreeNode", BsTreeNode.Parse },
            { "NiSwitchNode", NiSwitchNode.Parse },
            { "NiExtraData", NiExtraData.Parse },
            { "NiIntegerExtraData", NiIntegerExtraData.Parse },
            { "BSXFlags", BsxFlags.Parse },
            { "NiTriShape", NiTriShape.Parse },
            { "NiTriShapeData", NiTriShapeData.Parse },
            { "BSLODTriShape", BsLodTriShape.Parse },
            { "BSMultiBoundNode", BsMultiBoundNode.Parse },
            { "BSLightingShaderProperty", BsLightingShaderProperty.Parse },
            { "BSShaderTextureSet", BsShaderTextureSet.Parse },
            { "NiAlphaProperty", NiAlphaProperty.Parse },
            { "bhkCollisionObject", BhkCollisionObject.Parse },
            { "bhkRigidBody", BhkRigidBody.Parse },
            { "bhkRigidBodyT", BhkRigidBodyT.Parse },
            { "bhkCompressedMeshShape", BhkCompressedMeshShape.Parse },
            { "bhkCompressedMeshShapeData", BhkCompressedMeshShapeData.Parse },
            { "bhkMoppBvTreeShape", BhkMoppBvTreeShape.Parse },
            { "bhkListShape", BhkListShape.Parse },
            { "bhkConvexVerticesShape", BhkConvexVerticesShape.Parse }
        };
    }

    public class NiFile
    {
        public string Name { get; private set; }
        public Header Header { get; private set; }
        public List<NiObject> NiObjects { get; private set; } = new();
        public Footer Footer { get; private set; }

        private NiFile(string name, Header header)
        {
            Name = name;
            Header = header;
        }

        public static NiFile ReadNif(string fileName, BinaryReader nifReader, long startPosition)
        {
            var name = Path.GetFileName(fileName);
            nifReader.BaseStream.Seek(startPosition, SeekOrigin.Begin);
            var header = Header.ParseHeader(nifReader);
            var niFile = new NiFile(name, header);
            for (var i = 0; i < header.NumberOfBlocks; i++)
            {
                var blockType = header.BlockTypes[header.BlockTypeIndex[i]];

                if (NiObjectParsers.ParseFunctions.TryGetValue(blockType, out var parseFunction))
                {
                    niFile.NiObjects.Add(parseFunction(nifReader, blockType, header));
                }
                else
                {
                    Logger.LogWarning(
                        $"NIF Reader({fileName}): Unsupported NiObject type: {header.BlockTypes[header.BlockTypeIndex[i]]}");
                    if (header.BlockSizes != null)
                    {
                        nifReader.BaseStream.Seek(header.BlockSizes[i], SeekOrigin.Current);
                        niFile.NiObjects.Add(new UnsupportedNiObject(header.BlockTypes[header.BlockTypeIndex[i]]));
                    }
                    else
                    {
                        throw new InvalidDataException(
                            $"NIF Reader({fileName}): Block sizes are not present, could not skip the unsupported block.");
                    }
                }
            }

            niFile.Footer = Footer.ParseFooter(nifReader);

            return niFile;
        }
    }
}