using System.Collections.Generic;
using Textures;
using UnityEngine;

namespace Engine
{
    public enum ScreenResolution
    {
        Native,
        HalfNative,
        QuarterNative
    }

    public enum TargetFrameRate
    {
        FPS_30 = 30,
        FPS_60 = 60,
        FPS_72 = 72,
        FPS_90 = 90,
        FPS_120 = 120,
        FPS_144 = 144,
        FPS_240 = 240,
        FPS_REFRESH_RATE = 1000
    }

    public enum ShadowQuality
    {
        Disable,
        Low,
        Medium,
        High,
        VeryHigh
    }

    public static class Settings
    {
        public static string DataPath { get; private set; }

        private const string DataPathKey = "DataPath";

        public static List<string> LoadOrder { get; private set; }

        private const string LoadOrderKey = "LoadOrder";

        private const char LoadOrderSeparator = '/';

        public static TextureResolution TextureResolution { get; private set; } = TextureResolution.Half;

        private const string TextureResolutionKey = "TextureResolution";

        public static ScreenResolution ScreenResolution { get; private set; } = ScreenResolution.HalfNative;

        private const string ScreenResolutionKey = "ScreenResolution";

        private static int _nativeScreenWidth;

        private static int _nativeScreenHeight;

        private static int _nativeScreenRefreshRate;

        public static TargetFrameRate TargetFrameRate { get; private set; } = TargetFrameRate.FPS_60;

        private const string TargetFrameRateKey = "TargetFrameRate";

        public static AnisotropicFiltering AnisotropicFiltering { get; private set; } = AnisotropicFiltering.Disable;

        private const string AnisotropicFilteringKey = "AnisotropicFiltering";

        public static ShadowQuality ShadowQuality { get; private set; } = ShadowQuality.Disable;

        private const string ShadowQualityKey = "ShadowQuality";

        public static float LoadingDesiredWorkTimePerFrame { get; private set; } = 1.0f / 100;
        
        private const string LoadingDesiredWorkTimePerFrameKey = "LoadingDesiredWorkTimePerFrame";

        public static float InGameDesiredWorkTimePerFrame { get; private set; } = 1.0f / 1000;
        
        private const string InGameDesiredWorkTimePerFrameKey = "InGameDesiredWorkTimePerFrame";

        public static void Initialize()
        {
            if (PlayerPrefs.HasKey(DataPathKey))
            {
                DataPath = PlayerPrefs.GetString(DataPathKey);
            }

            if (PlayerPrefs.HasKey(LoadOrderKey))
            {
                LoadOrder = new List<string>(PlayerPrefs.GetString(LoadOrderKey).Split(LoadOrderSeparator));
            }

            if (PlayerPrefs.HasKey(TextureResolutionKey))
            {
                TextureResolution = (TextureResolution)PlayerPrefs.GetInt(TextureResolutionKey);
            }

            _nativeScreenWidth = Screen.currentResolution.width;
            _nativeScreenHeight = Screen.currentResolution.height;
            _nativeScreenRefreshRate = Screen.currentResolution.refreshRate;
            if (PlayerPrefs.HasKey(ScreenResolutionKey))
            {
                ScreenResolution = (ScreenResolution)PlayerPrefs.GetInt(ScreenResolutionKey);
            }

            ApplyScreenResolution();

            if (PlayerPrefs.HasKey(TargetFrameRateKey))
            {
                TargetFrameRate = (TargetFrameRate)PlayerPrefs.GetInt(TargetFrameRateKey);
            }

            ApplyTargetFrameRate();

            if (PlayerPrefs.HasKey(AnisotropicFilteringKey))
            {
                AnisotropicFiltering = (AnisotropicFiltering)PlayerPrefs.GetInt(AnisotropicFilteringKey);
            }

            QualitySettings.anisotropicFiltering = AnisotropicFiltering;

            if (PlayerPrefs.HasKey(ShadowQualityKey))
            {
                ShadowQuality = (ShadowQuality)PlayerPrefs.GetInt(ShadowQualityKey);
            }

            ApplyShadowQuality();
            
            if (PlayerPrefs.HasKey(LoadingDesiredWorkTimePerFrameKey))
            {
                LoadingDesiredWorkTimePerFrame = PlayerPrefs.GetFloat(LoadingDesiredWorkTimePerFrameKey);
            }
            
            if (PlayerPrefs.HasKey(InGameDesiredWorkTimePerFrameKey))
            {
                InGameDesiredWorkTimePerFrame = PlayerPrefs.GetFloat(InGameDesiredWorkTimePerFrameKey);
            }
        }

        public static void SetDataPath(string path)
        {
            PlayerPrefs.SetString(DataPathKey, path);
            PlayerPrefs.Save();
            DataPath = path;
        }

        public static void SetLoadOrder(List<string> loadOrder)
        {
            PlayerPrefs.SetString(LoadOrderKey, string.Join(LoadOrderSeparator, loadOrder));
            PlayerPrefs.Save();
            LoadOrder = loadOrder;
        }

        public static void SetTextureResolution(TextureResolution resolution)
        {
            PlayerPrefs.SetInt(TextureResolutionKey, (int)resolution);
            PlayerPrefs.Save();
            TextureResolution = resolution;
        }

        public static void SetScreenResolution(ScreenResolution resolution)
        {
            PlayerPrefs.SetInt(ScreenResolutionKey, (int)resolution);
            PlayerPrefs.Save();
            ScreenResolution = resolution;
            ApplyScreenResolution();
        }

        private static void ApplyScreenResolution()
        {
            switch (ScreenResolution)
            {
                case ScreenResolution.Native:
                    Screen.SetResolution(_nativeScreenWidth, _nativeScreenHeight, true);
                    break;
                case ScreenResolution.HalfNative:
                    Screen.SetResolution(_nativeScreenWidth / 2, _nativeScreenHeight / 2, true);
                    break;
                case ScreenResolution.QuarterNative:
                    Screen.SetResolution(_nativeScreenWidth / 4, _nativeScreenHeight / 4, true);
                    break;
            }
        }

        public static void SetTargetFrameRate(TargetFrameRate frameRate)
        {
            PlayerPrefs.SetInt(TargetFrameRateKey, (int)frameRate);
            PlayerPrefs.Save();
            TargetFrameRate = frameRate;
            ApplyTargetFrameRate();
        }

        private static void ApplyTargetFrameRate()
        {
            if (TargetFrameRate == TargetFrameRate.FPS_REFRESH_RATE)
            {
                Application.targetFrameRate = _nativeScreenRefreshRate;
            }
            else
            {
                Application.targetFrameRate = (int)TargetFrameRate;
            }
        }

        public static void SetAnisotropicFiltering(AnisotropicFiltering filtering)
        {
            PlayerPrefs.SetInt(AnisotropicFilteringKey, (int)filtering);
            PlayerPrefs.Save();
            AnisotropicFiltering = filtering;
            QualitySettings.anisotropicFiltering = filtering;
        }

        public static void SetShadowQuality(ShadowQuality quality)
        {
            PlayerPrefs.SetInt(ShadowQualityKey, (int)quality);
            PlayerPrefs.Save();
            ShadowQuality = quality;
            ApplyShadowQuality();
        }

        private static void ApplyShadowQuality()
        {
            switch (ShadowQuality)
            {
                case ShadowQuality.Disable:
                    QualitySettings.shadowmaskMode = ShadowmaskMode.Shadowmask;
                    QualitySettings.shadows = UnityEngine.ShadowQuality.Disable;
                    QualitySettings.shadowResolution = ShadowResolution.Low;
                    QualitySettings.shadowProjection = ShadowProjection.StableFit;
                    QualitySettings.shadowDistance = 0;
                    QualitySettings.shadowNearPlaneOffset = 3;
                    QualitySettings.shadowCascades = 0;
                    break;
                case ShadowQuality.Low:
                    QualitySettings.shadowmaskMode = ShadowmaskMode.Shadowmask;
                    QualitySettings.shadows = UnityEngine.ShadowQuality.HardOnly;
                    QualitySettings.shadowResolution = ShadowResolution.Low;
                    QualitySettings.shadowProjection = ShadowProjection.StableFit;
                    QualitySettings.shadowDistance = 20;
                    QualitySettings.shadowNearPlaneOffset = 3;
                    QualitySettings.shadowCascades = 0;
                    break;
                case ShadowQuality.Medium:
                    QualitySettings.shadowmaskMode = ShadowmaskMode.DistanceShadowmask;
                    QualitySettings.shadows = UnityEngine.ShadowQuality.All;
                    QualitySettings.shadowResolution = ShadowResolution.Medium;
                    QualitySettings.shadowProjection = ShadowProjection.StableFit;
                    QualitySettings.shadowDistance = 40;
                    QualitySettings.shadowNearPlaneOffset = 3;
                    QualitySettings.shadowCascades = 2;
                    QualitySettings.shadowCascade2Split = 0.333f;
                    break;
                case ShadowQuality.High:
                    QualitySettings.shadowmaskMode = ShadowmaskMode.DistanceShadowmask;
                    QualitySettings.shadows = UnityEngine.ShadowQuality.All;
                    QualitySettings.shadowResolution = ShadowResolution.High;
                    QualitySettings.shadowProjection = ShadowProjection.StableFit;
                    QualitySettings.shadowDistance = 70;
                    QualitySettings.shadowNearPlaneOffset = 3;
                    QualitySettings.shadowCascades = 2;
                    QualitySettings.shadowCascade2Split = 0.333f;
                    break;
                case ShadowQuality.VeryHigh:
                    QualitySettings.shadowmaskMode = ShadowmaskMode.DistanceShadowmask;
                    QualitySettings.shadows = UnityEngine.ShadowQuality.All;
                    QualitySettings.shadowResolution = ShadowResolution.VeryHigh;
                    QualitySettings.shadowProjection = ShadowProjection.StableFit;
                    QualitySettings.shadowDistance = 150;
                    QualitySettings.shadowNearPlaneOffset = 3;
                    QualitySettings.shadowCascades = 4;
                    QualitySettings.shadowCascade4Split = new Vector3(0.067f, 0.2f, 0.467f);
                    break;
            }
        }
        
        public static void SetLoadingDesiredWorkTimePerFrame(float time)
        {
            PlayerPrefs.SetFloat(LoadingDesiredWorkTimePerFrameKey, time);
            PlayerPrefs.Save();
            LoadingDesiredWorkTimePerFrame = time;
        }
        
        public static void SetInGameDesiredWorkTimePerFrame(float time)
        {
            PlayerPrefs.SetFloat(InGameDesiredWorkTimePerFrameKey, time);
            PlayerPrefs.Save();
            InGameDesiredWorkTimePerFrame = time;
        }
    }
}