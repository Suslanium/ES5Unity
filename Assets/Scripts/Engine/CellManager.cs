using System.Collections;
using System.Collections.Generic;
using System.IO;
using Cinemachine;
using Core;
using MasterFile;
using MasterFile.MasterFileContents;
using MasterFile.MasterFileContents.Records;
using NIF.Converter;
using UnityEngine;
using UnityEngine.Rendering;
using Convert = Core.Convert;
using Object = UnityEngine.Object;

namespace Engine
{
    public class CellInfo
    {
        public GameObject CellGameObject { get; set; }
        
        public List<IEnumerator> ObjectCreationCoroutines { get; } = new();
    }

    public class CellManager
    {
        private readonly ESMasterFile _masterFile;
        private readonly NifManager _nifManager;
        private readonly TemporalLoadBalancer _temporalLoadBalancer;
        private readonly List<CellInfo> _cells = new();
        private Vector3 _tempPlayerPosition;
        private Quaternion _tempPlayerRotation;

        public CellManager(ESMasterFile masterFile, NifManager nifManager, TemporalLoadBalancer temporalLoadBalancer)
        {
            _masterFile = masterFile;
            _nifManager = nifManager;
            _temporalLoadBalancer = temporalLoadBalancer;
        }

        public void LoadInteriorCell(string editorID, bool persistentOnly = false)
        {
            var cellInfo = new CellInfo();
            var creationCoroutine = StartCellLoading(editorID, cellInfo, persistentOnly);
            cellInfo.ObjectCreationCoroutines.Add(creationCoroutine);
            _temporalLoadBalancer.AddTask(creationCoroutine);
            _cells.Add(cellInfo);
        }

        public void LoadInteriorCell(uint formID, bool persistentOnly = false)
        {
            var cellInfo = new CellInfo();
            var creationCoroutine = StartCellLoading(formID, cellInfo, persistentOnly);
            cellInfo.ObjectCreationCoroutines.Add(creationCoroutine);
            _temporalLoadBalancer.AddTask(creationCoroutine);
            _cells.Add(cellInfo);
        }

        private IEnumerator StartCellLoading(string editorId, CellInfo cellInfo, bool persistentOnly = false)
        {
            var cellTask = _masterFile.FindCellByEditorIDTask(editorId);
            while (!cellTask.IsCompleted)
            {
                yield return null;
            }

            var cell = cellTask.Result;

            var cellLoadingCoroutine = LoadInteriorCellRecord(cell, cellInfo, persistentOnly);
            while (cellLoadingCoroutine.MoveNext())
            {
                yield return null;
            }
        }

        private IEnumerator StartCellLoading(uint formId, CellInfo cellInfo, bool persistentOnly = false)
        {
            var cellTask = _masterFile.GetFromFormIDTask(formId);
            while (!cellTask.IsCompleted)
            {
                yield return null;
            }

            var cell = (CELL)cellTask.Result;

            var cellLoadingCoroutine = LoadInteriorCellRecord(cell, cellInfo, persistentOnly);
            while (cellLoadingCoroutine.MoveNext())
            {
                yield return null;
            }
        }


        private IEnumerator LoadInteriorCellRecord(CELL cell, CellInfo cellInfo, bool persistentOnly = false)
        {
            if ((cell.CellFlag & 0x0001) == 0)
                throw new InvalidDataException("Trying to load exterior cell as interior");
            
            var childrenTask = _masterFile.ReadNextTask();
            while (!childrenTask.IsCompleted)
            {
                yield return null;
            }

            var children = childrenTask.Result;
            if (children is not Group { GroupType: 6 } childrenGroup)
                throw new InvalidDataException("Cell children group not found");

            yield return null;
            
            var cellGameObject =
                new GameObject(string.IsNullOrEmpty(cell.EditorID) ? cell.FormID.ToString() : cell.EditorID);
            cellInfo.CellGameObject = cellGameObject;
            cellGameObject.SetActive(false);
            
            if (cell.CellLightingInfo != null)
            {
                var lightingCoroutine = ConfigureCellLighting(cell);
                while (lightingCoroutine.MoveNext())
                {
                    yield return null;
                }
            }
            
            foreach (var subGroup in childrenGroup.GroupData)
            {
                if (subGroup is not Group group) continue;
                if (group.GroupType != 8 && (group.GroupType != 9 || persistentOnly)) continue;
                
                var objectInstantiationTask = InstantiateCellReferences(group, cellGameObject);
                while (objectInstantiationTask.MoveNext())
                {
                    yield return null;
                }
            }

            var postProcessTask = PostProcessInteriorCell(cellGameObject);
            while (postProcessTask.MoveNext())
            {
                yield return null;
            }
        }

        private IEnumerator PostProcessInteriorCell(GameObject cellGameObject)
        {
            cellGameObject.SetActive(true);
            //TODO static batching causes a huge freeze
            StaticBatchingUtility.Combine(cellGameObject);
            yield return null;
            
            var player = GameObject.FindGameObjectWithTag("Player");
            yield return null;

            player.SetActive(false);
            player.transform.position = _tempPlayerPosition;
            player.transform.rotation = _tempPlayerRotation;
            player.SetActive(true);
            _tempPlayerPosition = Vector3.zero;
            _tempPlayerRotation = Quaternion.identity;
            yield return null;
        }

        private IEnumerator ConfigureCellLighting(CELL cellRecord)
        {
            LGTM template = null;
            if (cellRecord.LightingTemplateReference > 0)
            {
                var templateTask = _masterFile.GetFromFormIDTask(cellRecord.LightingTemplateReference);
                while (!templateTask.IsCompleted)
                {
                    yield return null;
                }

                template = (LGTM)templateTask.Result;
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

            if (Camera.main == null) yield break;
            var mainCamera = Camera.main;
            //This looks almost the same as forward rendering, but improves performance by a lot
            /*
                WARNING: The line below won't work from Unity version 2022.2.
                To fix this, you can either choose forward rendering (which will decrease performance by a lot) or choose deferred shading path.
                The main problem right now is that deferred shading looks really bad. The shaders probably need to be rewritten for deferred shading.
            */
            mainCamera.renderingPath = RenderingPath.DeferredLighting;
            if (!RenderSettings.fog) yield break;
            
            //The camera shouldn't render anything beyond the fog
            var farClipPlane = mainCamera.farClipPlane;
            var convFogEndDist = Mathf.Lerp(mainCamera.nearClipPlane, (farClipPlane),
                RenderSettings.fogEndDistance / farClipPlane);
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.backgroundColor = RenderSettings.fogColor;
            yield return null;
            var cineMachine = Camera.main.gameObject.GetComponent<CinemachineBrain>();
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

        private IEnumerator InstantiateCellReferences(Group referencesGroup, GameObject parent)
        {
            foreach (var entry in referencesGroup.GroupData)
            {
                if (entry is not Record record) continue;
                if (record is not REFR reference) continue;
                var referencedRecordTask = _masterFile.GetFromFormIDTask(reference.BaseObjectReference);
                while (!referencedRecordTask.IsCompleted)
                {
                    yield return null;
                }
                var referencedRecord = referencedRecordTask.Result;
                switch (referencedRecord)
                {
                    case STAT staticRecord:
                        if (staticRecord.FormID == 0x32)
                        {
                            _tempPlayerPosition = NifUtils.NifPointToUnityPoint(new Vector3(reference.Position[0],
                                reference.Position[1], reference.Position[2]));
                            _tempPlayerRotation = NifUtils.NifEulerAnglesToUnityQuaternion(
                                new Vector3(reference.Rotation[0], reference.Rotation[1], reference.Rotation[2]));
                            break;
                        }

                        _nifManager.PreloadNifFile(staticRecord.NifModelFilename);
                        break;
                    case MSTT movableStatic:
                        _nifManager.PreloadNifFile(movableStatic.NifModelFilename);
                        break;
                    case FURN furniture:
                        _nifManager.PreloadNifFile(furniture.NifModelFilename);
                        break;
                    case LIGH light:
                        if (!string.IsNullOrEmpty(light.NifModelFilename))
                            _nifManager.PreloadNifFile(light.NifModelFilename);
                        break;
                }

                yield return null;
            }

            yield return null;

            foreach (var entry in referencesGroup.GroupData)
            {
                if (entry is not Record record) continue;
                if (record is not REFR reference) continue;
                var referencedRecordTask = _masterFile.GetFromFormIDTask(reference.BaseObjectReference);
                while (!referencedRecordTask.IsCompleted)
                {
                    yield return null;
                }
                var referencedRecord = referencedRecordTask.Result;
                var objectInstantiationCoroutine = InstantiateCellObject(parent, reference, referencedRecord);
                if (objectInstantiationCoroutine == null) continue;
                while (objectInstantiationCoroutine.MoveNext())
                {
                    yield return null;
                }
            }
        }

        private IEnumerator InstantiateCellObject(GameObject parent, REFR referenceRecord, Record referencedRecord)
        {
            if (referencedRecord == null) return null;
            return referencedRecord switch
            {
                STAT staticRecord => InstantiateModelAtPositionAndRotation(staticRecord.NifModelFilename,
                    referenceRecord.Position, referenceRecord.Rotation, referenceRecord.Scale, parent),
                MSTT movableStatic => InstantiateModelAtPositionAndRotation(movableStatic.NifModelFilename,
                    referenceRecord.Position, referenceRecord.Rotation, referenceRecord.Scale, parent),
                FURN furniture => InstantiateModelAtPositionAndRotation(furniture.NifModelFilename,
                    referenceRecord.Position, referenceRecord.Rotation, referenceRecord.Scale, parent),
                LIGH light => InstantiateLightAtPositionAndRotation(referenceRecord, light, referenceRecord.Position,
                    referenceRecord.Rotation, referenceRecord.Scale, parent),
                _ => null
            };
        }

        private IEnumerator InstantiateModelAtPositionAndRotation(string modelPath, float[] position, float[] rotation,
            float scale, GameObject parent)
        {
            var modelObjectCoroutine = _nifManager.InstantiateNif(modelPath, modelObject =>
            {
                ApplyPositionAndRotation(position, rotation, scale, parent, modelObject);
            });
            while (modelObjectCoroutine.MoveNext())
            {
                yield return null;
            }
        }

        private IEnumerator InstantiateLightAtPositionAndRotation(REFR lightReference, LIGH lightRecord,
            float[] position,
            float[] rotation,
            float scale, GameObject parent)
        {
            GameObject modelObject = null;
            if (!string.IsNullOrEmpty(lightRecord.NifModelFilename))
            {
                var modelObjectCoroutine =
                    _nifManager.InstantiateNif(lightRecord.NifModelFilename, o => { modelObject = o; });
                while (modelObjectCoroutine.MoveNext())
                {
                    yield return null;
                }
            }

            if (modelObject == null)
                modelObject = new GameObject(lightRecord.EditorID);

            ApplyPositionAndRotation(position, rotation, scale, parent, modelObject);
            InstantiateLightOnGameObject(lightReference, lightRecord, modelObject);
        }

        private void InstantiateLightOnGameObject(REFR reference, LIGH lightRecord, GameObject gameObject)
        {
            if (gameObject == null) return;
            //Create separate gameObject and rotate it in case of a spot light
            if ((lightRecord.Flags & 0x0400) != 0)
            {
                var spotGameObject = new GameObject(gameObject.name)
                {
                    transform =
                    {
                        parent = gameObject.transform,
                        position = gameObject.transform.position,
                        rotation = Quaternion.LookRotation(Vector3.down)
                    }
                };
                gameObject = spotGameObject;
            }

            var light = gameObject.AddComponent<Light>();
            //For some interesting reason the actual radius shown in CK is Base light radius + XRDS value of REFR
            light.range = 2 * ((lightRecord.Radius + reference.Radius) / Convert.meterInMWUnits);
            light.color = new Color32(lightRecord.ColorRGBA[0], lightRecord.ColorRGBA[1], lightRecord.ColorRGBA[2],
                255);
            //Intensity in Unity != intensity in Skyrim
            light.intensity = lightRecord.Fade + reference.FadeOffset;
            if ((lightRecord.Flags & 0x0400) != 0)
            {
                light.type = LightType.Spot;
            }
            else if ((lightRecord.Flags & 0x0800) == 0 && (lightRecord.Flags & 0x1000) == 0)
            {
                light.shadows = LightShadows.None;
            }
        }

        private static void ApplyPositionAndRotation(float[] position, float[] rotation, float scale, GameObject parent,
            GameObject modelObject)
        {
            if (modelObject == null) return;
            if (scale != 0f)
            {
                modelObject.transform.localScale = Vector3.one * scale;
            }

            modelObject.transform.position +=
                NifUtils.NifPointToUnityPoint(new Vector3(position[0], position[1], position[2]));
            modelObject.transform.rotation *=
                NifUtils.NifEulerAnglesToUnityQuaternion(new Vector3(rotation[0], rotation[1], rotation[2]));
            modelObject.transform.parent = parent.transform;
        }

        public IEnumerator DestroyAllCells()
        {
            foreach (var cell in _cells)
            {
                if (cell.CellGameObject != null) Object.Destroy(cell.CellGameObject);
                yield return null;
                foreach (var task in cell.ObjectCreationCoroutines)
                {
                    _temporalLoadBalancer.CancelTask(task);
                }

                yield return null;
            }

            _cells.Clear();
        }
    }
}