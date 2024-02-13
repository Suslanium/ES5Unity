using System.Collections.Generic;
using System.IO;
using MasterFile.MasterFileContents.Records.Structures;
using UnityEngine;

namespace MasterFile.MasterFileContents.Records
{
    /// <summary>
    /// <para>REFR records are over 90% of all records. They are simply references, but they are references to anything at any point in time (relatively speaking, whether triggered or otherwise), at any location in the game, doing something specified, or nothing. They can have extra items, or extra flags attached to them to identify them as containers, important places/locations.</para>
    /// <para>Though there are a lot of fields for modifying various aspects of different things, the only fields required are a NAME (this is the main object we are referring to) and DATA (locational information).</para>
    /// </summary>
    public class REFR : Record
    {
        /// <summary>
        /// Editor ID
        /// </summary>
        public string EditorID { get; private set; }

        /// <summary>
        /// FormID of anything as the base object.
        /// </summary>
        public uint BaseObjectReference { get; private set; }

        /// <summary>
        /// x/y/z position
        /// Z is the up axis
        /// </summary>
        public float[] Position { get; private set; }

        /// <summary>
        /// x/y/z rotation (radians)
        /// Z is the up axis
        /// </summary>
        public float[] Rotation { get; private set; }

        /// <summary>
        /// Scale (setScale)
        /// </summary>
        public float Scale { get; private set; }

        /// <summary>
        /// ~1.25. Controls the radii on objects like lights.
        /// </summary>
        public float Radius { get; private set; }

        /// <summary>
        /// origin/dest REFR
        /// </summary>
        public PortalInfo PortalDestinations { get; private set; }

        /// <summary>
        /// Lighting template FormID
        /// </summary>
        public uint LightingTemplateReference { get; private set; }

        /// <summary>
        /// INAM is used differently in a different context
        /// </summary>
        public uint ImageSpaceReference { get; private set; }

        /// <summary>
        /// Emitted light FormID
        /// </summary>
        public uint EmittedLightReference { get; private set; }

        /// <summary>
        /// Door teleport(32-byte struct)
        /// </summary>
        public DoorTeleport DoorTeleport { get; private set; }

        /// <summary>
        /// LVLI FormID of objects using this as a base
        /// </summary>
        public uint LeveledItemBase { get; private set; }

        /// <summary>
        /// LCRT FormID
        /// </summary>
        public uint LocationReference { get; private set; }

        /// <summary>
        /// Fade Offset from Base Object
        /// </summary>
        public float FadeOffset { get; private set; }

        public Primitive Primitive { get; private set; }

        public List<uint> LinkedRoomFormIDs { get; private set; } = new();

        private REFR(string type, uint dataSize, uint flag, uint formID, ushort timestamp, ushort versionControlInfo,
            ushort internalRecordVersion, ushort unknownData) : base(type, dataSize, flag, formID, timestamp,
            versionControlInfo, internalRecordVersion, unknownData)
        {
        }

        public static REFR ParseSpecific(Record baseInfo, BinaryReader fileReader, long position)
        {
            var refr = new REFR(baseInfo.Type, baseInfo.DataSize, baseInfo.Flag, baseInfo.FormID, baseInfo.Timestamp,
                baseInfo.VersionControlInfo, baseInfo.InternalRecordVersion, baseInfo.UnknownData);
            while (fileReader.BaseStream.Position < position + baseInfo.DataSize)
            {
                var fieldType = new string(fileReader.ReadChars(4));
                var fieldSize = fileReader.ReadUInt16();
                switch (fieldType)
                {
                    case "EDID":
                        refr.EditorID = new string(fileReader.ReadChars(fieldSize));
                        break;
                    case "NAME":
                        refr.BaseObjectReference = fileReader.ReadUInt32();
                        break;
                    case "XPRM":
                        var boundsX = fileReader.ReadSingle();
                        var boundsY = fileReader.ReadSingle();
                        var boundsZ = fileReader.ReadSingle();
                        var colorR = fileReader.ReadSingle();
                        var colorG = fileReader.ReadSingle();
                        var colorB = fileReader.ReadSingle();
                        var unknownFloat = fileReader.ReadSingle();
                        var unknownInt = fileReader.ReadUInt32();
                        refr.Primitive = new Primitive(new Vector3(boundsX, boundsY, boundsZ),
                            new Color(colorR, colorG, colorB), unknownFloat, unknownInt);
                        break;
                    case "XPOD":
                        var originFormID = fileReader.ReadUInt32();
                        var destinationFormID = fileReader.ReadUInt32();
                        refr.PortalDestinations = new PortalInfo(originFormID, destinationFormID);
                        break;
                    case "LNAM":
                        refr.LightingTemplateReference = fileReader.ReadUInt32();
                        break;
                    case "INAM":
                        refr.ImageSpaceReference = fileReader.ReadUInt32();
                        break;
                    case "XLRM":
                        refr.LinkedRoomFormIDs.Add(fileReader.ReadUInt32());
                        break;
                    case "XEMI":
                        refr.EmittedLightReference = fileReader.ReadUInt32();
                        break;
                    case "XTEL":
                        refr.DoorTeleport = new DoorTeleport(fileReader.ReadUInt32(),
                            new[] { fileReader.ReadSingle(), fileReader.ReadSingle(), fileReader.ReadSingle() },
                            new[] { fileReader.ReadSingle(), fileReader.ReadSingle(), fileReader.ReadSingle() },
                            fileReader.ReadUInt32());
                        break;
                    case "XLIB":
                        refr.LeveledItemBase = fileReader.ReadUInt32();
                        break;
                    case "XLRT":
                        refr.LocationReference = fileReader.ReadUInt32();
                        break;
                    case "DATA":
                        refr.Position = new[]
                            { fileReader.ReadSingle(), fileReader.ReadSingle(), fileReader.ReadSingle() };
                        refr.Rotation = new[]
                            { fileReader.ReadSingle(), fileReader.ReadSingle(), fileReader.ReadSingle() };
                        break;
                    case "XSCL":
                        refr.Scale = fileReader.ReadSingle();
                        break;
                    case "XRDS":
                        refr.Radius = fileReader.ReadSingle();
                        break;
                    case "XLIG":
                        fileReader.BaseStream.Seek(4, SeekOrigin.Current);
                        refr.FadeOffset = fileReader.ReadSingle();
                        fileReader.BaseStream.Seek(fieldSize == 16 ? 8 : 12, SeekOrigin.Current);
                        break;
                    default:
                        fileReader.BaseStream.Seek(fieldSize, SeekOrigin.Current);
                        break;
                }
            }

            return refr;
        }
    }
}