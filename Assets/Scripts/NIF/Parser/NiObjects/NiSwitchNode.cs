using System.IO;

namespace NIF.Parser.NiObjects
{
    public class NiSwitchNode : NiNode
    {
        public ushort SwitchNodeFlags { get; private set; }

        public uint Index { get; private set; }

        private NiSwitchNode(NiNode niNode) : base(niNode.ShaderType, niNode.Name, niNode.ExtraDataListLength,
            niNode.ExtraDataListReferences, niNode.ControllerObjectReference, niNode.Flags, niNode.Translation,
            niNode.Rotation, niNode.Scale, niNode.PropertiesNumber, niNode.PropertiesReferences,
            niNode.CollisionObjectReference, niNode.NumberOfChildren, niNode.ChildrenReferences,
            niNode.NumberOfEffects, niNode.EffectReferences)
        {
        }

        public new static NiSwitchNode Parse(BinaryReader nifReader, string ownerObjectName, Header header)
        {
            var niNode = NiNode.Parse(nifReader, ownerObjectName, header);
            var node = new NiSwitchNode(niNode)
            {
                SwitchNodeFlags = header.Version >= 0x0A010000 ? nifReader.ReadUInt16() : (ushort) 3,
                Index = nifReader.ReadUInt32()
            };

            return node;
        }
    }
}