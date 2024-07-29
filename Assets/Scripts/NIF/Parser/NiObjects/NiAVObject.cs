using System.IO;
using NIF.Parser.NiObjects.Enums;
using NIF.Parser.NiObjects.Structures;
using Vector3 = NIF.Parser.NiObjects.Structures.Vector3;

namespace NIF.Parser.NiObjects
{
    /// <summary>
    /// Abstract (again, not so abstract in this implementation) audio-visual base class from which all of Gamebryo's scene graph objects inherit.
    /// </summary>
    public class NiAvObject : NiObjectNet
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

        private NiAvObject(BsLightingShaderType shaderType, string name, uint extraDataListLength,
            int[] extraDataListReferences, int controllerObjectReference) : base(shaderType, name, extraDataListLength,
            extraDataListReferences, controllerObjectReference)
        {
        }

        protected NiAvObject(BsLightingShaderType shaderType, string name, uint extraDataListLength,
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

        protected new static NiAvObject Parse(BinaryReader nifReader, string ownerObjectName, Header header)
        {
            var ancestor = NiObjectNet.Parse(nifReader, ownerObjectName, header);
            var niAvObject = new NiAvObject(ancestor.ShaderType, ancestor.Name, ancestor.ExtraDataListLength,
                ancestor.ExtraDataListReferences, ancestor.ControllerObjectReference)
            {
                Flags = header.BethesdaVersion > 26 ? nifReader.ReadUInt32() : nifReader.ReadUInt16(),
                Translation = Vector3.Parse(nifReader),
                Rotation = Matrix33.Parse(nifReader),
                Scale = nifReader.ReadSingle()
            };

            if (Conditions.NiBsLteFo3(header))
            {
                niAvObject.PropertiesNumber = nifReader.ReadUInt32();
                niAvObject.PropertiesReferences = NifReaderUtils.ReadRefArray(nifReader, niAvObject.PropertiesNumber);
            }

            niAvObject.CollisionObjectReference = NifReaderUtils.ReadRef(nifReader);
            return niAvObject;
        }
    }
}