using System.IO;
using NIF.NiObjects.Enums;
using NIF.NiObjects.Structures;

namespace NIF.NiObjects
{
    /// <summary>
    /// Bethesda-specific fade node.
    /// </summary>
    public class BsFadeNode : NiNode
    {
        private BsFadeNode(BsLightingShaderType shaderType, string name, uint extraDataListLength,
            int[] extraDataListReferences, int controllerObjectReference, uint flags, Vector3 translation,
            Matrix33 rotation, float scale, uint propertiesNumber, int[] propertiesReferences,
            int collisionObjectReference, uint numberOfChildren, int[] childrenReferences, uint numberOfEffects,
            int[] effectReferences) : base(shaderType, name, extraDataListLength, extraDataListReferences,
            controllerObjectReference, flags, translation, rotation, scale, propertiesNumber, propertiesReferences,
            collisionObjectReference, numberOfChildren, childrenReferences, numberOfEffects, effectReferences)
        {
        }

        public new static BsFadeNode Parse(BinaryReader nifReader, string ownerObjectName, Header header)
        {
            var niNode = NiNode.Parse(nifReader, ownerObjectName, header);
            return new BsFadeNode(niNode.ShaderType, niNode.Name, niNode.ExtraDataListLength,
                niNode.ExtraDataListReferences, niNode.ControllerObjectReference, niNode.Flags, niNode.Translation,
                niNode.Rotation, niNode.Scale, niNode.PropertiesNumber, niNode.PropertiesReferences,
                niNode.CollisionObjectReference, niNode.NumberOfChildren, niNode.ChildrenReferences,
                niNode.NumberOfEffects, niNode.EffectReferences);
        }
    }
}