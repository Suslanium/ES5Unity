using System.IO;
namespace NIF.Parser.NiObjects
{
    public class BsTreeNode : NiNode
    {
        public int[] BoneRefs { get; private set; }
        
        public int[] BoneRefs1 { get; private set; }
        
        private BsTreeNode(NiNode niNode) : base(niNode.ShaderType, niNode.Name, niNode.ExtraDataListLength,
            niNode.ExtraDataListReferences, niNode.ControllerObjectReference, niNode.Flags, niNode.Translation,
            niNode.Rotation, niNode.Scale, niNode.PropertiesNumber, niNode.PropertiesReferences,
            niNode.CollisionObjectReference, niNode.NumberOfChildren, niNode.ChildrenReferences,
            niNode.NumberOfEffects, niNode.EffectReferences)
        {
        }
        
        public new static BsTreeNode Parse(BinaryReader nifReader, string ownerObjectName, Header header)
        {
            var niNode = NiNode.Parse(nifReader, ownerObjectName, header);
            var node = new BsTreeNode(niNode);
            
            var numBones = nifReader.ReadUInt32();
            node.BoneRefs = NifReaderUtils.ReadRefArray(nifReader, numBones);
            
            var numBones1 = nifReader.ReadUInt32();
            node.BoneRefs1 = NifReaderUtils.ReadRefArray(nifReader, numBones1);
            
            return node;
        }
    }
}