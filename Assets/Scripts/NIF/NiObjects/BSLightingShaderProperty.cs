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
    public class BSLightingShaderProperty : BSShaderProperty
    {
        public uint ShaderPropertyFlags1 { get; private set; }
        public uint ShaderPropertyFlags2 { get; private set; }
        public uint F76ShaderType { get; private set; }
        public uint NumSF1 { get; private set; }
        public uint NumSF2 { get; private set; }
        public uint[] SF1 { get; private set; }
        public uint[] SF2 { get; private set; }
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
        public float FO476RimLightPower { get; private set; }
        public float FO476BackLightPower { get; private set; }
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
        public bool UseSSR { get; private set; }

        public Color3 SkinTintColor { get; private set; }

        //FO4/F76
        public float SkinTintAlpha { get; private set; }
        public Color3 HairTintColor { get; private set; }
        public float MaxPasses { get; private set; }
        public float Scale { get; private set; }
        public float ParallaxInnerLayerThickness { get; private set; }
        public float ParallaxRefractionScale { get; private set; }
        public TexCoord ParallaxInnerLayerTextureScale { get; private set; }

        public float ParallaxEnvmapStrength { get; private set; }

        //Sparkle snow params skipped
        public float EyeCubemapScale { get; private set; }
        public Vector3 LeftEyeReflectionCenter { get; private set; }
        public Vector3 RightEyeReflectionCenter { get; private set; }

        private BSLightingShaderProperty(BSLightingShaderType shaderType, string name, uint extraDataListLength,
            int[] extraDataListReferences, int controllerObjectReference, ushort shadeFlags, uint fo3ShaderType,
            uint fo3ShaderFlags, uint fo3ShaderFlags2, float fo3EnvMapScale) : base(shaderType, name,
            extraDataListLength, extraDataListReferences, controllerObjectReference, shadeFlags, fo3ShaderType,
            fo3ShaderFlags, fo3ShaderFlags2, fo3EnvMapScale)
        {
        }

        public new static BSLightingShaderProperty Parse(BinaryReader nifReader, string ownerObjectName, Header header)
        {
            var ancestor = BSShaderProperty.Parse(nifReader, ownerObjectName, header);
            var bsShaderProperty = new BSLightingShaderProperty(ancestor.ShaderType, ancestor.Name,
                ancestor.ExtraDataListLength,
                ancestor.ExtraDataListReferences, ancestor.ControllerObjectReference, ancestor.ShadeFlags,
                ancestor.FO3ShaderType, ancestor.FO3ShaderFlags, ancestor.FO3ShaderFlags2, ancestor.FO3EnvMapScale);
            if (header.BethesdaVersion <= 139)
            {
                bsShaderProperty.ShaderPropertyFlags1 = nifReader.ReadUInt32();
                bsShaderProperty.ShaderPropertyFlags2 = nifReader.ReadUInt32();
            }

            if (header.BethesdaVersion == 155)
            {
                bsShaderProperty.F76ShaderType = nifReader.ReadUInt32();
            }

            if (header.BethesdaVersion >= 132)
            {
                bsShaderProperty.NumSF1 = nifReader.ReadUInt32();
                if (header.BethesdaVersion >= 152)
                {
                    bsShaderProperty.NumSF2 = nifReader.ReadUInt32();
                }
                bsShaderProperty.SF1 = NIFReaderUtils.ReadUintArray(nifReader, bsShaderProperty.NumSF1);
                if (header.BethesdaVersion >= 152)
                {
                    bsShaderProperty.SF2 = NIFReaderUtils.ReadUintArray(nifReader, bsShaderProperty.NumSF2);
                }
            }
            
            bsShaderProperty.UVOffset = TexCoord.Parse(nifReader);
            bsShaderProperty.UVScale = TexCoord.Parse(nifReader);
            bsShaderProperty.TextureSetReference = NIFReaderUtils.ReadRef(nifReader);
            bsShaderProperty.EmissiveColor = Color3.Parse(nifReader);
            bsShaderProperty.EmissiveMultiple = nifReader.ReadSingle();
            if (header.BethesdaVersion >= 130)
            {
                bsShaderProperty.RootMaterial = NIFReaderUtils.ReadString(nifReader, header);
            }

            bsShaderProperty.TextureClampMode = nifReader.ReadUInt32();
            bsShaderProperty.Alpha = nifReader.ReadSingle();
            bsShaderProperty.RefractionStrength = nifReader.ReadSingle();

            if (header.BethesdaVersion < 130)
            {
                bsShaderProperty.Glossiness = nifReader.ReadSingle();
            }
            else
            {
                bsShaderProperty.Smoothness = nifReader.ReadSingle();
            }

            bsShaderProperty.SpecularColor = Color3.Parse(nifReader);
            bsShaderProperty.SpecularStrength = nifReader.ReadSingle();
            if (header.BethesdaVersion < 130)
            {
                bsShaderProperty.LightingEffect1 = nifReader.ReadSingle();
                bsShaderProperty.LightingEffect2 = nifReader.ReadSingle();
            }

            if (header.BethesdaVersion is >= 130 and <= 139)
            {
                bsShaderProperty.SubsurfaceRollOff = nifReader.ReadSingle();
                bsShaderProperty.FO476RimLightPower = nifReader.ReadSingle();
                if (bsShaderProperty.FO476RimLightPower >= 3.402823466e+38 &&
                    bsShaderProperty.FO476RimLightPower < float.MaxValue)
                {
                    bsShaderProperty.FO476BackLightPower = nifReader.ReadSingle();
                }
            }

            if (header.BethesdaVersion >= 130)
            {
                bsShaderProperty.GrayScaleToPaletteScale = nifReader.ReadSingle();
                bsShaderProperty.FresnelPower = nifReader.ReadSingle();
                // Skipping wetness
                nifReader.BaseStream.Seek(12, SeekOrigin.Current);
                if (header.BethesdaVersion == 130) nifReader.BaseStream.Seek(4, SeekOrigin.Current);
                nifReader.BaseStream.Seek(8, SeekOrigin.Current);
                if (header.BethesdaVersion > 130) nifReader.BaseStream.Seek(4, SeekOrigin.Current);
                if (header.BethesdaVersion == 155) nifReader.BaseStream.Seek(4, SeekOrigin.Current);
                // Skipping luminance
                nifReader.BaseStream.Seek(16, SeekOrigin.Current);
                //Skipping translucency
                bsShaderProperty.DoTranslucency = nifReader.ReadBoolean();
                if (bsShaderProperty.DoTranslucency)
                {
                    nifReader.BaseStream.Seek(22, SeekOrigin.Current);
                }

                if (header.BethesdaVersion == 155)
                {
                    var hasTexArrays = nifReader.ReadBoolean();
                    if (hasTexArrays)
                    {
                        throw new NotImplementedException("FO76 texture arrays not implemented yet");
                    }
                }
            }

            if (header.BethesdaVersion <= 139 && bsShaderProperty.ShaderType == BSLightingShaderType.EnvironmentMap)
            {
                bsShaderProperty.EnvironmentMapScale = nifReader.ReadSingle();
            }

            if (header.BethesdaVersion is >= 130 and <= 139 &&
                bsShaderProperty.ShaderType == BSLightingShaderType.EnvironmentMap)
            {
                bsShaderProperty.UseScreenSpaceReflections = nifReader.ReadBoolean();
                bsShaderProperty.UseSSR = nifReader.ReadBoolean();
            }

            if (header.BethesdaVersion == 155)
            {
                throw new NotImplementedException("FO76 skin/hair tint is not implemented yet");
            }

            if (header.BethesdaVersion <= 139 && bsShaderProperty.ShaderType == BSLightingShaderType.SkinTint)
            {
                bsShaderProperty.SkinTintColor = Color3.Parse(nifReader);
            }

            if (header.BethesdaVersion is >= 130 and <= 139 &&
                bsShaderProperty.ShaderType == BSLightingShaderType.SkinTint)
            {
                bsShaderProperty.SkinTintAlpha = nifReader.ReadSingle();
            }
            if (header.BethesdaVersion <= 139 && bsShaderProperty.ShaderType == BSLightingShaderType.HairTint)
            {
                bsShaderProperty.HairTintColor = Color3.Parse(nifReader);
            }

            if (bsShaderProperty.ShaderType == BSLightingShaderType.ParallaxOcc)
            {
                bsShaderProperty.MaxPasses = nifReader.ReadSingle();
                bsShaderProperty.Scale = nifReader.ReadSingle();
            }

            if (bsShaderProperty.ShaderType == BSLightingShaderType.MultiLayerParallax)
            {
                bsShaderProperty.ParallaxInnerLayerThickness = nifReader.ReadSingle();
                bsShaderProperty.ParallaxRefractionScale = nifReader.ReadSingle();
                bsShaderProperty.ParallaxInnerLayerTextureScale = TexCoord.Parse(nifReader);
                bsShaderProperty.ParallaxEnvmapStrength = nifReader.ReadSingle();
            }

            if (bsShaderProperty.ShaderType == BSLightingShaderType.SparkleSnow)
            {
                //Skipping
                nifReader.BaseStream.Seek(16, SeekOrigin.Current);
            }

            if (bsShaderProperty.ShaderType == BSLightingShaderType.EyeEnvmap)
            {
                bsShaderProperty.EyeCubemapScale = nifReader.ReadSingle();
                bsShaderProperty.LeftEyeReflectionCenter = Vector3.Parse(nifReader);
                bsShaderProperty.RightEyeReflectionCenter = Vector3.Parse(nifReader);
            }
            return bsShaderProperty;
        }
    }
}