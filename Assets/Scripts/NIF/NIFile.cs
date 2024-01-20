using System.Collections.Generic;
using System.IO;
using NIF.NiObjects;
using UnityEngine;

namespace NIF
{
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
                switch (header.BlockTypes[header.BlockTypeIndex[i]])
                {
                    case "NiNode":
                        niFile.NiObjects.Add(NiNode.Parse(nifReader, "NiNode", header));
                        break;
                    case "BSFadeNode":
                        niFile.NiObjects.Add(BsFadeNode.Parse(nifReader, "BSFadeNode", header));
                        break;
                    case "NiExtraData":
                        niFile.NiObjects.Add(NiExtraData.Parse(nifReader, "NiExtraData", header));
                        break;
                    case "NiIntegerExtraData":
                        niFile.NiObjects.Add(NiIntegerExtraData.Parse(nifReader, "NiIntegerExtraData", header));
                        break;
                    case "BSXFlags":
                        niFile.NiObjects.Add(BsxFlags.Parse(nifReader, "BSXFlags", header));
                        break;
                    case "NiTriShape":
                        niFile.NiObjects.Add(NiTriShape.Parse(nifReader, "NiTriShape", header));
                        break;
                    case "NiTriShapeData":
                        niFile.NiObjects.Add(NiTriShapeData.Parse(nifReader, "NiTriShapeData", header));
                        break;
                    case "BSLODTriShape":
                        niFile.NiObjects.Add(BsLodTriShape.Parse(nifReader, "BSLODTriShape", header));
                        break;
                    case "BSMultiBoundNode":
                        niFile.NiObjects.Add(BsMultiBoundNode.Parse(nifReader, "BSMultiBoundNode", header));
                        break;
                    case "BSLightingShaderProperty":
                        niFile.NiObjects.Add(BsLightingShaderProperty.Parse(nifReader, "BSLightingShaderProperty",
                            header));
                        break;
                    case "BSShaderTextureSet":
                        niFile.NiObjects.Add(BsShaderTextureSet.Parse(nifReader, "BSShaderTextureSet", header));
                        break;
                    case "NiAlphaProperty":
                        niFile.NiObjects.Add(NiAlphaProperty.Parse(nifReader, "NiAlphaProperty", header));
                        break;
                    case "bhkCollisionObject":
                        niFile.NiObjects.Add(BhkCollisionObject.Parse(nifReader, "bhkCollisionObject", header));
                        break;
                    case "bhkRigidBody":
                        niFile.NiObjects.Add(BhkRigidBody.Parse(nifReader, "bhkRigidBody", header));
                        break;
                    case "bhkRigidBodyT":
                        niFile.NiObjects.Add(BhkRigidBodyT.Parse(nifReader, "bhkRigidBody", header));
                        break;
                    default:
                        Debug.LogWarning(
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

                        break;
                }
            }

            niFile.Footer = Footer.ParseFooter(nifReader);

            return niFile;
        }
    }
}