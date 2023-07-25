using System;
using System.IO;
using NIF.NiObjects.Enums;
using NIF.NiObjects.Structures;

namespace NIF.NiObjects
{
    /// <summary>
    /// Bethesda shader property for Skyrim and later.
    /// (Some stuff is skipped there because it wasn't needed or I was just lazy/tired of writing readers for these humongous structures)
    /// Warning: This class is pretty messy
    /// </summary>
    public class BsLightingShaderProperty : BsShaderProperty
    {
        public uint ShaderPropertyFlags1 { get; private set; }
        public uint ShaderPropertyFlags2 { get; private set; }
        public uint F76ShaderType { get; private set; }
        //(Shader flags are stored in arrays in later versions)
        public uint NumSf1 { get; private set; }
        public uint NumSf2 { get; private set; }
        public uint[] Sf1 { get; private set; }
        public uint[] Sf2 { get; private set; }
        public TexCoord UVOffset { get; private set; }
        public TexCoord UVScale { get; private set; }
        public int TextureSetReference { get; private set; }
        public Color3 EmissiveColor { get; private set; }
        public float EmissiveMultiple { get; private set; }
        public string RootMaterial { get; private set; }
        public uint TextureClampMode { get; private set; }
        public float Alpha { get; private set; }
        public float RefractionStrength { get; private set; }
        public float Glossiness { get; private set; }
        public float Smoothness { get; private set; }
        public Color3 SpecularColor { get; private set; }
        public float SpecularStrength { get; private set; }
        public float LightingEffect1 { get; private set; }
        public float LightingEffect2 { get; private set; }
        public float SubsurfaceRollOff { get; private set; }
        public float Fo476RimLightPower { get; private set; }
        public float Fo476BackLightPower { get; private set; }
        public float GrayScaleToPaletteScale { get; private set; }

        public float FresnelPower { get; private set; }

        //FO4 and 76 wetness and luminance params are skipped coz Im lazy
        public bool DoTranslucency { get; private set; }

        //F76 translucency params are skipped for the same reason
        //F76 texture arrays are skipped
        public float EnvironmentMapScale { get; private set; }

        //FO4
        public bool UseScreenSpaceReflections { get; private set; }

        //FO4
        public bool UseSsr { get; private set; }

        public Color3 SkinTintColor { get; private set; }

        //FO4/F76
        public float SkinTintAlpha { get; private set; }
        public Color3 HairTintColor { get; private set; }
        public float MaxPasses { get; private set; }
        public float Scale { get; private set; }
        public float ParallaxInnerLayerThickness { get; private set; }
        public float ParallaxRefractionScale { get; private set; }
        public TexCoord ParallaxInnerLayerTextureScale { get; private set; }

        public float ParallaxEnvMapStrength { get; private set; }

        //Sparkle snow params skipped
        public float EyeCubemapScale { get; private set; }
        public Vector3 LeftEyeReflectionCenter { get; private set; }
        public Vector3 RightEyeReflectionCenter { get; private set; }

        private BsLightingShaderProperty(BsLightingShaderType shaderType, string name, uint extraDataListLength,
            int[] extraDataListReferences, int controllerObjectReference, ushort shadeFlags, uint fo3ShaderType,
            uint fo3ShaderFlags, uint fo3ShaderFlags2, float fo3EnvMapScale) : base(shaderType, name,
            extraDataListLength, extraDataListReferences, controllerObjectReference, shadeFlags, fo3ShaderType,
            fo3ShaderFlags, fo3ShaderFlags2, fo3EnvMapScale)
        {
        }

        public new static BsLightingShaderProperty Parse(BinaryReader nifReader, string ownerObjectName, Header header)
        {
            var ancestor = BsShaderProperty.Parse(nifReader, ownerObjectName, header);
            var bsShaderProperty = new BsLightingShaderProperty(ancestor.ShaderType, ancestor.Name,
                ancestor.ExtraDataListLength,
                ancestor.ExtraDataListReferences, ancestor.ControllerObjectReference, ancestor.ShadeFlags,
                ancestor.Fo3ShaderType, ancestor.Fo3ShaderFlags, ancestor.Fo3ShaderFlags2, ancestor.Fo3EnvMapScale);
            if (Conditions.NiBsLteFo4(header))
            {
                bsShaderProperty.ShaderPropertyFlags1 = nifReader.ReadUInt32();
                bsShaderProperty.ShaderPropertyFlags2 = nifReader.ReadUInt32();
            }

            if (Conditions.BsF76(header))
            {
                bsShaderProperty.F76ShaderType = nifReader.ReadUInt32();
            }

            if (Conditions.BsGte132(header))
            {
                bsShaderProperty.NumSf1 = nifReader.ReadUInt32();
                if (Conditions.BsGte152(header))
                {
                    bsShaderProperty.NumSf2 = nifReader.ReadUInt32();
                }
                bsShaderProperty.Sf1 = NifReaderUtils.ReadUintArray(nifReader, bsShaderProperty.NumSf1);
                if (Conditions.BsGte152(header))
                {
                    bsShaderProperty.Sf2 = NifReaderUtils.ReadUintArray(nifReader, bsShaderProperty.NumSf2);
                }
            }
            
            bsShaderProperty.UVOffset = TexCoord.Parse(nifReader);
            bsShaderProperty.UVScale = TexCoord.Parse(nifReader);
            bsShaderProperty.TextureSetReference = NifReaderUtils.ReadRef(nifReader);
            bsShaderProperty.EmissiveColor = Color3.Parse(nifReader);
            bsShaderProperty.EmissiveMultiple = nifReader.ReadSingle();
            if (Conditions.BsGte130(header))
            {
                bsShaderProperty.RootMaterial = NifReaderUtils.ReadString(nifReader, header);
            }

            bsShaderProperty.TextureClampMode = nifReader.ReadUInt32();
            bsShaderProperty.Alpha = nifReader.ReadSingle();
            bsShaderProperty.RefractionStrength = nifReader.ReadSingle();

            if (Conditions.NiBsLtFo4(header))
            {
                bsShaderProperty.Glossiness = nifReader.ReadSingle();
            }
            else
            {
                bsShaderProperty.Smoothness = nifReader.ReadSingle();
            }

            bsShaderProperty.SpecularColor = Color3.Parse(nifReader);
            bsShaderProperty.SpecularStrength = nifReader.ReadSingle();
            if (Conditions.NiBsLtFo4(header))
            {
                bsShaderProperty.LightingEffect1 = nifReader.ReadSingle();
                bsShaderProperty.LightingEffect2 = nifReader.ReadSingle();
            }

            if (Conditions.BsFo4_2(header))
            {
                bsShaderProperty.SubsurfaceRollOff = nifReader.ReadSingle();
                bsShaderProperty.Fo476RimLightPower = nifReader.ReadSingle();
                if (bsShaderProperty.Fo476RimLightPower >= 3.402823466e+38 &&
                    bsShaderProperty.Fo476RimLightPower < float.MaxValue)
                {
                    bsShaderProperty.Fo476BackLightPower = nifReader.ReadSingle();
                }
            }

            if (Conditions.BsGte130(header))
            {
                bsShaderProperty.GrayScaleToPaletteScale = nifReader.ReadSingle();
                bsShaderProperty.FresnelPower = nifReader.ReadSingle();
                // Skipping wetness
                nifReader.BaseStream.Seek(12, SeekOrigin.Current);
                if (Conditions.BsFo4(header)) nifReader.BaseStream.Seek(4, SeekOrigin.Current);
                nifReader.BaseStream.Seek(8, SeekOrigin.Current);
                if (Conditions.BsGt130(header)) nifReader.BaseStream.Seek(4, SeekOrigin.Current);
                if (Conditions.BsF76(header)) nifReader.BaseStream.Seek(4, SeekOrigin.Current);
                // Skipping luminance
                nifReader.BaseStream.Seek(16, SeekOrigin.Current);
                //Skipping translucency
                bsShaderProperty.DoTranslucency = nifReader.ReadBoolean();
                if (bsShaderProperty.DoTranslucency)
                {
                    nifReader.BaseStream.Seek(22, SeekOrigin.Current);
                }

                if (Conditions.BsF76(header))
                {
                    var hasTexArrays = nifReader.ReadBoolean();
                    if (hasTexArrays)
                    {
                        throw new NotImplementedException("FO76 texture arrays not implemented yet");
                    }
                }
            }

            if (Conditions.NiBsLteFo4(header) && bsShaderProperty.ShaderType == BsLightingShaderType.EnvironmentMap)
            {
                bsShaderProperty.EnvironmentMapScale = nifReader.ReadSingle();
            }

            if (Conditions.BsFo4_2(header) &&
                bsShaderProperty.ShaderType == BsLightingShaderType.EnvironmentMap)
            {
                bsShaderProperty.UseScreenSpaceReflections = nifReader.ReadBoolean();
                bsShaderProperty.UseSsr = nifReader.ReadBoolean();
            }

            if (Conditions.BsF76(header))
            {
                throw new NotImplementedException("FO76 skin/hair tint is not implemented yet");
            }

            if (Conditions.NiBsLteFo4(header) && bsShaderProperty.ShaderType == BsLightingShaderType.SkinTint)
            {
                bsShaderProperty.SkinTintColor = Color3.Parse(nifReader);
            }

            if (Conditions.BsFo4_2(header) &&
                bsShaderProperty.ShaderType == BsLightingShaderType.SkinTint)
            {
                bsShaderProperty.SkinTintAlpha = nifReader.ReadSingle();
            }
            if (Conditions.NiBsLteFo4(header) && bsShaderProperty.ShaderType == BsLightingShaderType.HairTint)
            {
                bsShaderProperty.HairTintColor = Color3.Parse(nifReader);
            }

            if (bsShaderProperty.ShaderType == BsLightingShaderType.ParallaxOcc)
            {
                bsShaderProperty.MaxPasses = nifReader.ReadSingle();
                bsShaderProperty.Scale = nifReader.ReadSingle();
            }

            if (bsShaderProperty.ShaderType == BsLightingShaderType.MultiLayerParallax)
            {
                bsShaderProperty.ParallaxInnerLayerThickness = nifReader.ReadSingle();
                bsShaderProperty.ParallaxRefractionScale = nifReader.ReadSingle();
                bsShaderProperty.ParallaxInnerLayerTextureScale = TexCoord.Parse(nifReader);
                bsShaderProperty.ParallaxEnvMapStrength = nifReader.ReadSingle();
            }

            if (bsShaderProperty.ShaderType == BsLightingShaderType.SparkleSnow)
            {
                //Skipping
                nifReader.BaseStream.Seek(16, SeekOrigin.Current);
            }

            if (bsShaderProperty.ShaderType == BsLightingShaderType.EyeEnvmap)
            {
                bsShaderProperty.EyeCubemapScale = nifReader.ReadSingle();
                bsShaderProperty.LeftEyeReflectionCenter = Vector3.Parse(nifReader);
                bsShaderProperty.RightEyeReflectionCenter = Vector3.Parse(nifReader);
            }
            return bsShaderProperty;
        }
    }
}