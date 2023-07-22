using System.IO;
using NIF.NiObjects;
using UnityEngine;

namespace NIF
{
    public static class NIFReader
    {
        public static NIFile ReadNIF(string fileName, BinaryReader nifReader, long startPosition)
        {
            nifReader.BaseStream.Seek(startPosition, SeekOrigin.Begin);
            Header header = Header.ParseHeader(nifReader);
            var niFile = new NIFile(header);
            for (int i = 0; i < header.NumberOfBlocks; i++)
            {
                switch (header.BlockTypes[header.BlockTypeIndex[i]])
                {
                    case "NiNode":
                        niFile.NiObjects.Add(NiNode.Parse(nifReader, "NiNode", header));
                        break;
                    case "BSFadeNode":
                        niFile.NiObjects.Add(BSFadeNode.Parse(nifReader, "BSFadeNode", header));
                        break;
                    case "NiExtraData":
                        niFile.NiObjects.Add(NiExtraData.Parse(nifReader, "NiExtraData", header));
                        break;
                    case "NiIntegerExtraData":
                        niFile.NiObjects.Add(NiIntegerExtraData.Parse(nifReader, "NiIntegerExtraData", header));
                        break;
                    case "BSXFlags":
                        niFile.NiObjects.Add(BSXFlags.Parse(nifReader, "BSXFlags", header));
                        break;
                    case "NiTriShape":
                        niFile.NiObjects.Add(NiTriShape.Parse(nifReader, "NiTriShape", header));
                        break;
                    default:
                        Debug.LogWarning($"NIF Reader({fileName}): Unsupported NiObject type: {header.BlockTypes[header.BlockTypeIndex[i]]}");
                        if (header.BlockSizes != null)
                        {
                            nifReader.BaseStream.Seek(header.BlockSizes[i], SeekOrigin.Current);
                        }
                        else
                        {
                            throw new InvalidDataException($"NIF Reader({fileName}): Block sizes are not present, could not skip the unsupported block.");
                        }
                        break;
                }
            }

            return niFile;
        }
    }
}