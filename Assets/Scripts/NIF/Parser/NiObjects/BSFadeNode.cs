using System.IO;

namespace NIF.Parser.NiObjects
{
    /// <summary>
    /// Bethesda-specific fade node.
    /// </summary>
    public class BsFadeNode : NiNode
    {
        private BsFadeNode(NiNode niNode) : base(niNode.ShaderType, niNode.Name, niNode.ExtraDataListLength,
            niNode.ExtraDataListReferences, niNode.ControllerObjectReference, niNode.Flags, niNode.Translation,
            niNode.Rotation, niNode.Scale, niNode.PropertiesNumber, niNode.PropertiesReferences,
            niNode.CollisionObjectReference, niNode.NumberOfChildren, niNode.ChildrenReferences,
            niNode.NumberOfEffects, niNode.EffectReferences)
        {
        }

        public new static BsFadeNode Parse(BinaryReader nifReader, string ownerObjectName, Header header)
        {
            var niNode = NiNode.Parse(nifReader, ownerObjectName, header);
            return new BsFadeNode(niNode);
        }
    }
}