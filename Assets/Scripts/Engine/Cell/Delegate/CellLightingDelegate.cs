using System.Collections;
using Cinemachine;
using Engine.Cell.Delegate.Interfaces;
using Engine.Core;
using Engine.MasterFile;
using MasterFile.MasterFileContents.Records;
using NIF.Builder;
using UnityEngine;
using UnityEngine.Rendering;

namespace Engine.Cell.Delegate
{
    /// <summary>
    /// Configures the overall lighting of the cell.
    /// (Ambient color, fog, etc.)
    /// </summary>
    public class CellLightingDelegate : ICellPostProcessDelegate, ICellDestroyDelegate
    {
        private const int DefaultCameraFarPlane = 500;
        private readonly GameEngine _gameEngine;
        private readonly MasterFileManager _masterFileManager;
        
        public CellLightingDelegate(GameEngine gameEngine, MasterFileManager masterFileManager)
        {
            _gameEngine = gameEngine;
            _masterFileManager = masterFileManager;
        }
        
        public IEnumerator PostProcessCell(CELL cell, GameObject cellGameObject)
        {
            if (cell.CellLightingInfo == null) yield break;
            //Don't process lighting for exterior cells
            if ((cell.CellFlag & 0x0001) == 0) yield break;
            var lightingCoroutine = ConfigureCellLighting(cell);
            while (lightingCoroutine.MoveNext())
            {
                yield return null;
            }
        }

        public IEnumerator OnDestroy()
        {
            return ResetLighting();
        }
        
        private IEnumerator ResetLighting()
        {
            RenderSettings.ambientMode = AmbientMode.Skybox;
            var directionalLight = RenderSettings.sun;
            directionalLight.enabled = true;
            directionalLight.color = new Color(1f, 0.9568627f, 0.8392157f);
            directionalLight.transform.rotation = Quaternion.Euler(50, -270, 0);
            RenderSettings.fog = false;
            yield return null;
            if (_gameEngine.MainCamera == null) yield break;
            var mainCamera = _gameEngine.MainCamera;
            mainCamera.renderingPath = RenderingPath.DeferredLighting;
            mainCamera.clearFlags = CameraClearFlags.Skybox;
            yield return null;
            var cineMachine = mainCamera.gameObject.GetComponent<CinemachineBrain>();
            if (cineMachine != null)
            {
                cineMachine.ActiveVirtualCamera.VirtualCameraGameObject.GetComponent<CinemachineVirtualCamera>()
                    .m_Lens.FarClipPlane = DefaultCameraFarPlane;
            }
            else
            {
                mainCamera.farClipPlane = DefaultCameraFarPlane;
            }
        }
        
        private IEnumerator ConfigureCellLighting(CELL cellRecord)
        {
            LGTM template = null;
            if (cellRecord.LightingTemplateReference > 0)
            {
                var templateTask = _masterFileManager.GetFromFormIDTask(cellRecord.LightingTemplateReference);
                while (!templateTask.IsCompleted)
                {
                    yield return null;
                }

                if (templateTask.Result is LGTM lgtm)
                    template = lgtm;
            }

            var directionalLight = RenderSettings.sun;

            //Inherit ambient color
            RenderSettings.ambientMode = AmbientMode.Flat;
            if ((cellRecord.CellLightingInfo.InheritFlags & 0x0001) != 0 && template != null)
            {
                RenderSettings.ambientLight = new Color32(template.LightingData.AmbientRGBA[0],
                    template.LightingData.AmbientRGBA[1], template.LightingData.AmbientRGBA[2], 255);
            }
            else
            {
                RenderSettings.ambientLight = new Color32(cellRecord.CellLightingInfo.AmbientRGBA[0],
                    cellRecord.CellLightingInfo.AmbientRGBA[1], cellRecord.CellLightingInfo.AmbientRGBA[2], 255);
            }

            yield return null;

            //Inherit directional color
            if ((cellRecord.CellLightingInfo.InheritFlags & 0x0002) != 0 && template != null)
            {
                var directionalColor = new Color32(template.LightingData.DirectionalRGBA[0],
                    template.LightingData.DirectionalRGBA[1], template.LightingData.DirectionalRGBA[2], 255);
                if (directionalColor != Color.black)
                {
                    directionalLight.enabled = true;
                    directionalLight.color = directionalColor;
                    var rotation = Quaternion.identity;
                    rotation *= NifUtils.NifEulerAnglesToUnityQuaternion(
                        new Vector3(template.LightingData.DirectionalRotationXY,
                            template.LightingData.DirectionalRotationXY, template.LightingData.DirectionalRotationZ));
                    directionalLight.transform.rotation = rotation;
                }
                else
                {
                    directionalLight.enabled = false;
                }
            }
            else
            {
                var directionalColor = new Color32(cellRecord.CellLightingInfo.DirectionalRGBA[0],
                    cellRecord.CellLightingInfo.DirectionalRGBA[1], cellRecord.CellLightingInfo.DirectionalRGBA[2],
                    255);
                if (directionalColor != Color.black)
                {
                    directionalLight.enabled = true;
                    directionalLight.color = directionalColor;
                    var rotation = Quaternion.identity;
                    rotation *= NifUtils.NifEulerAnglesToUnityQuaternion(
                        new Vector3(cellRecord.CellLightingInfo.DirectionalRotationXY,
                            cellRecord.CellLightingInfo.DirectionalRotationXY,
                            cellRecord.CellLightingInfo.DirectionalRotationZ));
                    directionalLight.transform.rotation = rotation;
                }
                else
                {
                    directionalLight.enabled = false;
                }
            }

            yield return null;

            //Inherit fog far distance
            if ((cellRecord.CellLightingInfo.InheritFlags & 0x0010) != 0 && template != null)
            {
                if (template.LightingData.FogFar > 0)
                {
                    RenderSettings.fog = true;
                    RenderSettings.fogMode = FogMode.Linear;
                    RenderSettings.fogEndDistance = template.LightingData.FogFar / Convert.meterInMWUnits;
                }
                else
                {
                    RenderSettings.fog = false;
                }
            }
            else
            {
                if (cellRecord.CellLightingInfo.FogFar > 0)
                {
                    RenderSettings.fog = true;
                    RenderSettings.fogMode = FogMode.Linear;
                    RenderSettings.fogEndDistance = cellRecord.CellLightingInfo.FogFar / Convert.meterInMWUnits;
                }
                else
                {
                    RenderSettings.fog = false;
                }
            }

            yield return null;

            //Inherit fog near distance
            if (RenderSettings.fog && (cellRecord.CellLightingInfo.InheritFlags & 0x0008) != 0 && template != null)
            {
                RenderSettings.fogStartDistance = template.LightingData.FogNear / Convert.meterInMWUnits;
            }
            else if (RenderSettings.fog)
            {
                RenderSettings.fogStartDistance = cellRecord.CellLightingInfo.FogNear / Convert.meterInMWUnits;
            }

            yield return null;

            //Inherit fog color
            if (RenderSettings.fog && (cellRecord.CellLightingInfo.InheritFlags & 0x0004) != 0 && template != null)
            {
                RenderSettings.fogColor = new Color32(template.LightingData.FogNearColor[0],
                    template.LightingData.FogNearColor[1], template.LightingData.FogNearColor[2], 255);
            }
            else if (RenderSettings.fog)
            {
                RenderSettings.fogColor = new Color32(cellRecord.CellLightingInfo.FogNearColor[0],
                    cellRecord.CellLightingInfo.FogNearColor[1], cellRecord.CellLightingInfo.FogNearColor[2], 255);
            }

            yield return null;

            if (_gameEngine.MainCamera == null) yield break;
            var mainCamera = _gameEngine.MainCamera;
            //This looks almost the same as forward rendering, but improves performance by a lot
            /*
                WARNING: The line below won't work from Unity version 2022.2.
                To fix this, you can either choose forward rendering (which will decrease performance by a lot) or choose deferred shading path.
                The main problem right now is that deferred shading looks really bad. The shaders probably need to be rewritten for deferred shading.
            */
            mainCamera.renderingPath = RenderingPath.DeferredLighting;
            if (!RenderSettings.fog) yield break;

            //The camera shouldn't render anything beyond the fog
            var convFogEndDist = Mathf.Lerp(mainCamera.nearClipPlane, (DefaultCameraFarPlane),
                RenderSettings.fogEndDistance / DefaultCameraFarPlane);
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.backgroundColor = RenderSettings.fogColor;
            yield return null;
            var cineMachine = mainCamera.gameObject.GetComponent<CinemachineBrain>();
            if (cineMachine != null)
            {
                cineMachine.ActiveVirtualCamera.VirtualCameraGameObject.GetComponent<CinemachineVirtualCamera>()
                    .m_Lens.FarClipPlane = convFogEndDist;
            }
            else
            {
                mainCamera.farClipPlane = convFogEndDist;
            }
        }
    }
}