using System;
using System.Globalization;
using Textures;
using TMPro;
using UnityEngine;

namespace Engine.UI
{
    public class SettingsMenu : MonoBehaviour
    {
        [SerializeField] private TMP_Dropdown textureQualityDropdown;
        [SerializeField] private TMP_Dropdown resolutionDropdown;
        [SerializeField] private TMP_Dropdown targetFrameRateDropdown;
        [SerializeField] private TMP_Dropdown anisotropicFilteringDropdown;
        [SerializeField] private TMP_Dropdown shadowQualityDropdown;
        [SerializeField] private TMP_Dropdown monitoringDropdown;
        [SerializeField] private TMP_InputField loadWorkTimePerFrameInputField;
        [SerializeField] private TMP_InputField inGameWorkTimePerFrameInputField;

        private static readonly TargetFrameRate[] TargetFrameRateList = (TargetFrameRate[])Enum.GetValues(typeof(TargetFrameRate));

        public void Initialize()
        {
            textureQualityDropdown.onValueChanged.AddListener(value =>
            {
                Settings.SetTextureResolution((TextureResolution)value);
            });
            resolutionDropdown.onValueChanged.AddListener(value =>
            {
                Settings.SetScreenResolution((ScreenResolution)value);
            });
            targetFrameRateDropdown.onValueChanged.AddListener(value =>
            {
                var frameRate = TargetFrameRateList[value];
                Settings.SetTargetFrameRate(frameRate);
            });
            anisotropicFilteringDropdown.onValueChanged.AddListener(value =>
            {
                Settings.SetAnisotropicFiltering((AnisotropicFiltering)value);
            });
            shadowQualityDropdown.onValueChanged.AddListener(value =>
            {
                Settings.SetShadowQuality((ShadowQuality)value);
            });
            
            textureQualityDropdown.value = (int)Settings.TextureResolution;
            resolutionDropdown.value = (int)Settings.ScreenResolution;
            targetFrameRateDropdown.value = Array.IndexOf(TargetFrameRateList, Settings.TargetFrameRate);
            anisotropicFilteringDropdown.value = (int)Settings.AnisotropicFiltering;
            shadowQualityDropdown.value = (int)Settings.ShadowQuality;
            loadWorkTimePerFrameInputField.text =
                Settings.LoadingDesiredWorkTimePerFrame.ToString(CultureInfo.InvariantCulture);
            inGameWorkTimePerFrameInputField.text =
                Settings.InGameDesiredWorkTimePerFrame.ToString(CultureInfo.InvariantCulture);
        }

        private void OnDisable()
        {
            float.TryParse(loadWorkTimePerFrameInputField.text, out var loadWorkTimePerFrame);
            if (loadWorkTimePerFrame > 0)
                Settings.SetLoadingDesiredWorkTimePerFrame(loadWorkTimePerFrame);
            float.TryParse(inGameWorkTimePerFrameInputField.text, out var inGameWorkTimePerFrame);
            if (inGameWorkTimePerFrame > 0)
                Settings.SetInGameDesiredWorkTimePerFrame(inGameWorkTimePerFrame);
        }
    }
}