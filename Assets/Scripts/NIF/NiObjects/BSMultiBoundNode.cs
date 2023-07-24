using System.IO;
using NIF.NiObjects.Enums;
using NIF.NiObjects.Structures;

namespace NIF.NiObjects
{
    /// <summary>
    /// Bethesda-specific node.
    /// </summary>
    public class BSMultiBoundNode: NiNode
    {
        public int MultiBoundReference { get; private set; }
        public uint CullingMode { get; private set; }
        
        private BSMultiBoundNode(BSLightingShaderType shaderType, string name, uint extraDataListLength,
            int[] extraDataListReferences, int controllerObjectReference, uint flags, Vector3 translation,
            Matrix33 rotation, float scale, uint propertiesNumber, int[] propertiesReferences,
            int collisionObjectReference, uint numberOfChildren, int[] childrenReferences, uint numberOfEffects,
            int[] effectReferences) : base(shaderType, name, extraDataListLength, extraDataListReferences,
            controllerObjectReference, flags, translation, rotation, scale, propertiesNumber, propertiesReferences,
            collisionObjectReference, numberOfChildren, childrenReferences, numberOfEffects, effectReferences)
        {
        }

        public new static BSMultiBoundNode Parse(BinaryReader nifReader, string ownerObjectName, Header header)
        {
            var niNode = NiNode.Parse(nifReader, ownerObjectName, header);
            var node = new BSMultiBoundNode(niNode.ShaderType, niNode.Name, niNode.ExtraDataListLength,
                niNode.ExtraDataListReferences, niNode.ControllerObjectReference, niNode.Flags, niNode.Translation,
                niNode.Rotation, niNode.Scale, niNode.PropertiesNumber, niNode.PropertiesReferences,
                niNode.CollisionObjectReference, niNode.NumberOfChildren, niNode.ChildrenReferences,
                niNode.NumberOfEffects, niNode.EffectReferences)
            {
                MultiBoundReference = NIFReaderUtils.ReadRef(nifReader)
            };
            if (header.BethesdaVersion >= 83)
            {
                node.CullingMode = nifReader.ReadUInt32();
            }

            return node;
        }
    }
}