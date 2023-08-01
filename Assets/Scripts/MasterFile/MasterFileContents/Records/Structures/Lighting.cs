using System.IO;

namespace MasterFile.MasterFileContents.Records.Structures
{
    /// <summary>
    /// Lighting information
    /// </summary>
    public class Lighting
    {
        public byte[] AmbientRGBA { get; private set; }
        public byte[] DirectionalRGBA { get; private set; }
        public int DirectionalRotationXY { get; private set; }
        public int DirectionalRotationZ { get; private set; }
        public float DirectionalFade { get; private set; }
        public byte[] FogNearColor { get; private set; }
        public float FogNear { get; private set; }
        public float FogFar { get; private set; }
        public byte[] FogFarColor { get; private set; }
        public float FogMax { get; private set; }
        public float LightFadeDistanceStart { get; private set; }
        public float LightFadeDistanceEnd { get; private set; }

        /// <summary>
        /// Inherit flags - controls which parts are inherited from Lighting Template(only present in CELL records)
        /// <para>0x0001 - Ambient Color</para>
        /// <para>0x0002 - Directional Color</para>
        /// <para>0x0004 - Fog Color</para>
        /// <para>0x0008 - Fog Near</para>
        /// <para>0x0010 - Fog Far</para>
        /// <para>0x0020 - Directional Rot</para>
        /// <para>0x0040 - Directional Fade</para>
        /// <para>0x0080 - Clip Distance</para>
        /// <para>0x0100 - Fog Power</para>
        /// <para>0x0200 - Fog Max</para>
        /// <para>0x0400 - Light Fade Distances</para>
        /// </summary>
        public uint InheritFlags { get; private set; }

        private Lighting() {}
        
        /// <summary>
        /// ONLY to be used inside ParseSpecific function of CELL Record
        /// </summary>
        public static Lighting Parse(ushort fieldSize, BinaryReader fileReader)
        {
            if (fieldSize == 92)
            {
                Lighting lighting = new Lighting
                {
                    AmbientRGBA = fileReader.ReadBytes(4),
                    DirectionalRGBA = fileReader.ReadBytes(4),
                    FogNearColor = fileReader.ReadBytes(4),
                    FogNear = fileReader.ReadSingle(),
                    FogFar = fileReader.ReadSingle(),
                    DirectionalRotationXY = fileReader.ReadInt32(),
                    DirectionalRotationZ = fileReader.ReadInt32(),
                    DirectionalFade = fileReader.ReadSingle()
                };
                //Skip some unknown/unused info
                fileReader.BaseStream.Seek(40, SeekOrigin.Current);
                lighting.FogFarColor = fileReader.ReadBytes(4);
                lighting.FogMax = fileReader.ReadSingle();
                lighting.LightFadeDistanceStart = fileReader.ReadSingle();
                lighting.LightFadeDistanceEnd = fileReader.ReadSingle();
                lighting.InheritFlags = fileReader.ReadUInt32();
                return lighting;
            }
            else
            {
                fileReader.BaseStream.Seek(fieldSize, SeekOrigin.Current);
                return null;
            }
        }
    }
}