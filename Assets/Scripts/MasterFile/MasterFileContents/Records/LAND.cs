using System.Collections.Generic;
using System.IO;
using Engine.Core;
using JetBrains.Annotations;
using UnityEngine;

namespace MasterFile.MasterFileContents.Records
{
    public struct BaseTexture
    {
        public readonly uint LandTextureFormID;

        public readonly byte Quadrant;

        public BaseTexture(uint landTextureFormID, byte quadrant)
        {
            LandTextureFormID = landTextureFormID;
            Quadrant = quadrant;
        }
    }

    /// <summary>
    /// Exterior cell land data (terrain) record.
    /// </summary>
    public class LAND : Record
    {
        private const int LandSideLength = Convert.ExteriorCellSideLengthInSamples;

        /// <summary>
        /// Land vertex height map in game units.
        /// </summary>
        [CanBeNull]
        public float[,] VertexHeightMap { get; private set; }

        [CanBeNull] public Color[,] VertexColors { get; private set; }

        [CanBeNull] public Vector3[,] VertexNormals { get; private set; }

        public List<BaseTexture> BaseTextures { get; private set; } = new();

        private LAND(string type, uint dataSize, uint flag, uint formID, ushort timestamp, ushort versionControlInfo,
            ushort internalRecordVersion, ushort unknownData) : base(type, dataSize, flag, formID, timestamp,
            versionControlInfo, internalRecordVersion, unknownData)
        {
        }

        public static LAND ParseSpecific(Record baseInfo, BinaryReader fileReader, long position)
        {
            var land = new LAND(baseInfo.Type, baseInfo.DataSize, baseInfo.Flag, baseInfo.FormID, baseInfo.Timestamp,
                baseInfo.VersionControlInfo, baseInfo.InternalRecordVersion, baseInfo.UnknownData);
            while (fileReader.BaseStream.Position < position + baseInfo.DataSize)
            {
                var fieldType = new string(fileReader.ReadChars(4));
                var fieldSize = fileReader.ReadUInt16();
                switch (fieldType)
                {
                    case "VHGT":
                        var vertexHeightMap = new float[LandSideLength, LandSideLength];

                        var offset = fileReader.ReadSingle() * 8;
                        var rowOffset = 0;

                        for (var i = 0; i < LandSideLength * LandSideLength; i++)
                        {
                            var value = fileReader.ReadSByte() * 8;

                            var row = i / LandSideLength;
                            var column = i % LandSideLength;

                            if (column == 0)
                            {
                                rowOffset = 0;
                                offset += value;
                            }
                            else
                            {
                                rowOffset += value;
                            }

                            vertexHeightMap[row, column] = offset + rowOffset;
                        }

                        land.VertexHeightMap = vertexHeightMap;

                        fileReader.BaseStream.Seek(3, SeekOrigin.Current);
                        break;
                    case "VCLR":
                        var vertexColors = new Color[LandSideLength, LandSideLength];

                        for (var i = 0; i < LandSideLength * LandSideLength; i++)
                        {
                            var color = new Color(fileReader.ReadByte() / 255f, fileReader.ReadByte() / 255f,
                                fileReader.ReadByte() / 255f, 1f);

                            var row = i / LandSideLength;
                            var column = i % LandSideLength;

                            vertexColors[row, column] = color;
                        }

                        break;
                    case "VNML":
                        var vertexNormals = new Vector3[LandSideLength, LandSideLength];

                        for (var i = 0; i < LandSideLength * LandSideLength; i++)
                        {
                            var normal = new Vector3(fileReader.ReadSByte(), fileReader.ReadSByte(),
                                fileReader.ReadSByte());
                            normal = normal.normalized;

                            var row = i / LandSideLength;
                            var column = i % LandSideLength;

                            vertexNormals[row, column] = normal;
                        }

                        break;
                    case "BTXT":
                        var landTextureFormID = fileReader.ReadUInt32();
                        var quadrant = fileReader.ReadByte();
                        fileReader.BaseStream.Seek(3, SeekOrigin.Current);

                        land.BaseTextures.Add(new BaseTexture(landTextureFormID, quadrant));
                        break;
                    default:
                        fileReader.BaseStream.Seek(fieldSize, SeekOrigin.Current);
                        break;
                }
            }

            return land;
        }
    }
}