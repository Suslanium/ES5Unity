using System.IO;

namespace NIF.Parser.NiObjects
{
    public class BsLeafAnimNode : NiNode
    {
        private BsLeafAnimNode(NiNode niNode) : base(niNode.ShaderType, niNode.Name, niNode.ExtraDataListLength,
            niNode.ExtraDataListReferences, niNode.ControllerObjectReference, niNode.Flags, niNode.Translation,
            niNode.Rotation, niNode.Scale, niNode.PropertiesNumber, niNode.PropertiesReferences,
            niNode.CollisionObjectReference, niNode.NumberOfChildren, niNode.ChildrenReferences,
            niNode.NumberOfEffects, niNode.EffectReferences)
        {
        }

        public new static BsLeafAnimNode Parse(BinaryReader nifReader, string ownerObjectName, Header header)
        {
            var niNode = NiNode.Parse(nifReader, ownerObjectName, header);
            return new BsLeafAnimNode(niNode);
        }
    }
}