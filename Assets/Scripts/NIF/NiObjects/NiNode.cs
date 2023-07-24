﻿using System.IO;
using NIF.NiObjects.Enums;
using NIF.NiObjects.Structures;

namespace NIF.NiObjects
{
    /// <summary>
    /// Generic node object for grouping.
    /// </summary>
    public class NiNode : NiAVObject
    {
        /// <summary>
        /// The number of child objects.
        /// </summary>
        public uint NumberOfChildren { get; private set; }

        /// <summary>
        /// List of child node object indices.
        /// </summary>
        public int[] ChildrenReferences { get; private set; }

        /// <summary>
        /// The number of references to effect objects that follow.
        /// </summary>
        public uint NumberOfEffects { get; private set; }

        /// <summary>
        /// List of node effects.
        /// </summary>
        public int[] EffectReferences { get; private set; }

        private NiNode(BSLightingShaderType shaderType, string name, uint extraDataListLength,
            int[] extraDataListReferences, int controllerObjectReference, uint flags, Vector3 translation,
            Matrix33 rotation, float scale, uint propertiesNumber, int[] propertiesReferences,
            int collisionObjectReference) : base(shaderType, name, extraDataListLength, extraDataListReferences,
            controllerObjectReference, flags, translation, rotation, scale, propertiesNumber, propertiesReferences,
            collisionObjectReference)
        {
        }

        protected NiNode(BSLightingShaderType shaderType, string name, uint extraDataListLength,
            int[] extraDataListReferences, int controllerObjectReference, uint flags, Vector3 translation,
            Matrix33 rotation, float scale, uint propertiesNumber, int[] propertiesReferences,
            int collisionObjectReference, uint numberOfChildren, int[] childrenReferences, uint numberOfEffects,
            int[] effectReferences) : base(shaderType, name, extraDataListLength, extraDataListReferences,
            controllerObjectReference, flags, translation, rotation, scale, propertiesNumber, propertiesReferences,
            collisionObjectReference)
        {
            NumberOfChildren = numberOfChildren;
            ChildrenReferences = childrenReferences;
            NumberOfEffects = numberOfEffects;
            EffectReferences = effectReferences;
        }

        public new static NiNode Parse(BinaryReader nifReader, string ownerObjectName, Header header)
        {
            var ancestor = NiAVObject.Parse(nifReader, ownerObjectName, header);
            var niNode = new NiNode(ancestor.ShaderType, ancestor.Name, ancestor.ExtraDataListLength,
                ancestor.ExtraDataListReferences, ancestor.ControllerObjectReference, ancestor.Flags,
                ancestor.Translation, ancestor.Rotation, ancestor.Scale, ancestor.PropertiesNumber,
                ancestor.PropertiesReferences, ancestor.CollisionObjectReference);
            niNode.NumberOfChildren = nifReader.ReadUInt32();
            niNode.ChildrenReferences = NIFReaderUtils.ReadRefArray(nifReader, niNode.NumberOfChildren);

            if (header.BethesdaVersion >= 130) return niNode;
            niNode.NumberOfEffects = nifReader.ReadUInt32();
            niNode.EffectReferences = NIFReaderUtils.ReadRefArray(nifReader, niNode.NumberOfEffects);

            return niNode;
        }
    }
}