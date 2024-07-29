using System.IO;

namespace NIF.Parser.NiObjects
{
    /// <summary>
    /// <para>Controls animation and collision.  Integer holds flags:</para>
    /// <para>Bit 0 : enable havok, bAnimated(Skyrim)</para>
    /// <para>Bit 1 : enable collision, bHavok(Skyrim)</para>
    /// <para>Bit 2 : is skeleton nif?, bRagdoll(Skyrim)</para>
    /// <para>Bit 3 : enable animation, bComplex(Skyrim)</para>
    /// <para>Bit 4 : FlameNodes present, bAddon(Skyrim)</para>
    /// <para>Bit 5 : EditorMarkers present, bEditorMarker(Skyrim)</para>
    /// <para>Bit 6 : bDynamic(Skyrim)</para>
    /// <para>Bit 7 : bArticulated(Skyrim)</para>
    /// <para>Bit 8 : bIKTarget(Skyrim)/needsTransformUpdates</para>
    /// <para>Bit 9 : bExternalEmit(Skyrim)</para>
    /// <para>Bit 10: bMagicShaderParticles(Skyrim)</para>
    /// <para>Bit 11: bLights(Skyrim)</para>
    /// <para>Bit 12: bBreakable(Skyrim)</para>
    /// <para>Bit 13: bSearchedBreakable(Skyrim) .. Runtime only?</para>
    /// </summary>
    public class BsxFlags : NiIntegerExtraData
    {
        public BsxFlags(string name, uint integerData) : base(name, integerData)
        {
        }

        public new static BsxFlags Parse(BinaryReader nifReader, string ownerObjectName, Header header)
        {
            var niIntegerExtraData = NiIntegerExtraData.Parse(nifReader, ownerObjectName, header);
            return new BsxFlags(niIntegerExtraData.Name, niIntegerExtraData.IntegerData);
        }
    }
}