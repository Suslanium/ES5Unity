using System.IO;

namespace NIF.Parser.NiObjects
{
    /// <summary>
    /// Bethesda-specific node.
    /// </summary>
    public class BsMultiBoundNode: NiNode
    {
        public int MultiBoundReference { get; private set; }
        public uint CullingMode { get; private set; }
        
        private BsMultiBoundNode(NiNode niNode) : base(niNode.ShaderType, niNode.Name, niNode.ExtraDataListLength,
            niNode.ExtraDataListReferences, niNode.ControllerObjectReference, niNode.Flags, niNode.Translation,
            niNode.Rotation, niNode.Scale, niNode.PropertiesNumber, niNode.PropertiesReferences,
            niNode.CollisionObjectReference, niNode.NumberOfChildren, niNode.ChildrenReferences,
            niNode.NumberOfEffects, niNode.EffectReferences)
        {
        }

        public new static BsMultiBoundNode Parse(BinaryReader nifReader, string ownerObjectName, Header header)
        {
            var niNode = NiNode.Parse(nifReader, ownerObjectName, header);
            var node = new BsMultiBoundNode(niNode)
            {
                MultiBoundReference = NifReaderUtils.ReadRef(nifReader)
            };
            if (Conditions.BsGteSky(header))
            {
                node.CullingMode = nifReader.ReadUInt32();
            }

            return node;
        }
    }
}