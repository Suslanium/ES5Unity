using System.IO;
using NIF.NiObjects.Enums;
using NIF.NiObjects.Structures;
using UnityEngine;
using Vector3 = NIF.NiObjects.Structures.Vector3;

namespace NIF.NiObjects
{
    /// <summary>
    /// Abstract (again, not so abstract in this implementation) audio-visual base class from which all of Gamebryo's scene graph objects inherit.
    /// </summary>
    public class NiAVObject : NiObjectNET
    {
        /// <summary>
        /// (Well, I honestly don't know what these are for)
        /// </summary>
        public uint Flags { get; private set; }

        /// <summary>
        /// The translation vector.
        /// </summary>
        public Vector3 Translation { get; private set; }

        /// <summary>
        /// The rotation part of the transformation matrix.
        /// </summary>
        public Matrix33 Rotation { get; private set; }

        /// <summary>
        /// Scaling part (only uniform scaling is supported).
        /// </summary>
        public float Scale { get; private set; }

        public uint PropertiesNumber { get; private set; }

        /// <summary>
        /// All rendering properties attached to this object.(not present in games after Fallout 3)
        /// </summary>
        public int[] PropertiesReferences { get; private set; }

        public int CollisionObjectReference { get; private set; }

        private NiAVObject(BSLightingShaderType shaderType, string name, uint extraDataListLength,
            int[] extraDataListReferences, int controllerObjectReference) : base(shaderType, name, extraDataListLength,
            extraDataListReferences, controllerObjectReference)
        {
        }

        protected NiAVObject(BSLightingShaderType shaderType, string name, uint extraDataListLength,
            int[] extraDataListReferences, int controllerObjectReference, uint flags, Vector3 translation,
            Matrix33 rotation, float scale, uint propertiesNumber, int[] propertiesReferences,
            int collisionObjectReference) : base(shaderType, name, extraDataListLength, extraDataListReferences,
            controllerObjectReference)
        {
            Flags = flags;
            Translation = translation;
            Rotation = rotation;
            Scale = scale;
            PropertiesNumber = propertiesNumber;
            PropertiesReferences = propertiesReferences;
            CollisionObjectReference = collisionObjectReference;
        }

        protected new static NiAVObject Parse(BinaryReader nifReader, string ownerObjectName, Header header)
        {
            var ancestor = NiObjectNET.Parse(nifReader, ownerObjectName, header);
            var niAvObject = new NiAVObject(ancestor.ShaderType, ancestor.Name, ancestor.ExtraDataListLength,
                ancestor.ExtraDataListReferences, ancestor.ControllerObjectReference);
            niAvObject.Flags = header.BethesdaVersion > 26 ? nifReader.ReadUInt32() : nifReader.ReadUInt16();

            niAvObject.Translation = Vector3.Parse(nifReader);
            niAvObject.Rotation = Matrix33.Parse(nifReader);
            niAvObject.Scale = nifReader.ReadSingle();

            if (header.BethesdaVersion <= 34)
            {
                niAvObject.PropertiesNumber = nifReader.ReadUInt32();
                niAvObject.PropertiesReferences = NIFReaderUtils.ReadRefArray(nifReader, niAvObject.PropertiesNumber);
            }

            niAvObject.CollisionObjectReference = NIFReaderUtils.ReadRef(nifReader);
            return niAvObject;
        }
    }
}