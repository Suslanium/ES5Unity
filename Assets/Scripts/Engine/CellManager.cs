using System.Collections;
using System.Collections.Generic;
using System.IO;
using Core;
using MasterFile;
using MasterFile.MasterFileContents;
using MasterFile.MasterFileContents.Records;
using NIF.Converter;
using UnityEngine;
using UnityEngine.Rendering;

namespace Engine
{
    public class CellInfo
    {
        public GameObject CellGameObject { get; private set; }
        public List<IEnumerator> ObjectCreationCoroutines { get; private set; } = new();
        public CELL CellRecord { get; private set; }

        public CellInfo(GameObject cellGameObject, CELL cellRecord)
        {
            CellGameObject = cellGameObject;
            CellRecord = cellRecord;
        }
    }

    public class CellManager
    {
        private readonly ESMasterFile _masterFile;
        private readonly NifManager _nifManager;
        private readonly TemporalLoadBalancer _temporalLoadBalancer;
        private readonly List<CellInfo> _cells = new();

        public CellManager(ESMasterFile masterFile, NifManager nifManager, TemporalLoadBalancer temporalLoadBalancer)
        {
            _masterFile = masterFile;
            _nifManager = nifManager;
            _temporalLoadBalancer = temporalLoadBalancer;
        }

        public void LoadInteriorCell(string editorID, bool persistentOnly = false)
        {
            var cell = _masterFile.FindCellByEditorID(editorID);
            if ((cell.CellFlag & 0x0001) == 0)
                throw new InvalidDataException("Trying to load exterior cell as interior");
            var children = _masterFile.ReadNext();
            if (children is not Group { GroupType: 6 } childrenGroup)
                throw new InvalidDataException("Cell children group not found");
            var cellGameObject = new GameObject(editorID);
            var cellInfo = new CellInfo(cellGameObject, cell);
            ConfigureCellLighting(cell);
            foreach (var subGroup in childrenGroup.GroupData)
            {
                if (subGroup is not Group group) continue;
                if (group.GroupType == 8 || (group.GroupType == 9 && !persistentOnly))
                {
                    var objectInstantiationTask = InstantiateCellReferences(group, cellGameObject);
                    _temporalLoadBalancer.AddTask(objectInstantiationTask);
                    cellInfo.ObjectCreationCoroutines.Add(objectInstantiationTask);
                }
            }

            _cells.Add(cellInfo);
        }

        private void ConfigureCellLighting(CELL cellRecord)
        {
            LGTM template = null;
            if (cellRecord.LightingTemplateReference > 0)
                template = (LGTM)_masterFile.GetFromFormID(cellRecord.LightingTemplateReference);
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

            //Inherit fog near distance
            if (RenderSettings.fog && (cellRecord.CellLightingInfo.InheritFlags & 0x0008) != 0 && template != null)
            {
                RenderSettings.fogStartDistance = template.LightingData.FogNear / Convert.meterInMWUnits;
            }
            else if (RenderSettings.fog)
            {
                RenderSettings.fogStartDistance = cellRecord.CellLightingInfo.FogNear / Convert.meterInMWUnits;
            }

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
        }

        private IEnumerator InstantiateCellReferences(Group referencesGroup, GameObject parent)
        {
            foreach (var entry in referencesGroup.GroupData)
            {
                if (entry is not Record record) continue;
                if (record is not REFR reference) continue;
                var referencedRecord = _masterFile.GetFromFormID(reference.BaseObjectReference);
                switch (referencedRecord)
                {
                    case STAT staticRecord:
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
            }

            yield return null;

            foreach (var entry in referencesGroup.GroupData)
            {
                if (entry is not Record record) continue;
                if (record is not REFR reference) continue;
                var referencedRecord = _masterFile.GetFromFormID(reference.BaseObjectReference);
                InstantiateCellObject(parent, reference, referencedRecord);
                yield return null;
            }
        }

        private void InstantiateCellObject(GameObject parent, REFR referenceRecord, Record referencedRecord)
        {
            if (referencedRecord != null)
            {
                switch (referencedRecord)
                {
                    case STAT staticRecord:
                        InstantiateModelAtPositionAndRotation(staticRecord.NifModelFilename, referenceRecord.Position,
                            referenceRecord.Rotation, referenceRecord.Scale, parent);
                        break;
                    case MSTT movableStatic:
                        InstantiateModelAtPositionAndRotation(movableStatic.NifModelFilename, referenceRecord.Position,
                            referenceRecord.Rotation, referenceRecord.Scale, parent);
                        break;
                    case FURN furniture:
                        InstantiateModelAtPositionAndRotation(furniture.NifModelFilename, referenceRecord.Position,
                            referenceRecord.Rotation, referenceRecord.Scale, parent);
                        break;
                    case LIGH light:
                        InstantiateLightAtPositionAndRotation(referenceRecord, light, referenceRecord.Position,
                            referenceRecord.Rotation,
                            referenceRecord.Scale, parent);
                        break;
                }
            }
        }

        private GameObject InstantiateModelAtPositionAndRotation(string modelPath, float[] position, float[] rotation,
            float scale, GameObject parent)
        {
            var modelObject = _nifManager.InstantiateNif(modelPath);
            ApplyPositionAndRotation(position, rotation, scale, parent, modelObject);

            return modelObject;
        }

        private GameObject InstantiateLightAtPositionAndRotation(REFR lightReference, LIGH lightRecord,
            float[] position,
            float[] rotation,
            float scale, GameObject parent)
        {
            GameObject modelObject = null;
            if (!string.IsNullOrEmpty(lightRecord.NifModelFilename))
                modelObject = _nifManager.InstantiateNif(lightRecord.NifModelFilename);
            if (modelObject == null)
                modelObject = new GameObject(lightRecord.EditorID);

            InstantiateLightOnGameObject(lightReference, lightRecord, modelObject);
            ApplyPositionAndRotation(position, rotation, scale, parent, modelObject);

            return modelObject;
        }

        private void InstantiateLightOnGameObject(REFR reference, LIGH lightRecord, GameObject gameObject)
        {
            if (gameObject == null) return;
            var light = gameObject.AddComponent<Light>();
            //For some interesting reason the actual radius shown in CK is Base light radius + XRDS value of refr
            light.range = (lightRecord.Radius + reference.Radius) / Convert.meterInMWUnits;
            light.color = new Color32(lightRecord.ColorRGBA[0], lightRecord.ColorRGBA[1], lightRecord.ColorRGBA[2],
                255);
            //Intensity in Unity != intensity in Skyrim
            light.intensity = 2 * (lightRecord.Fade + reference.FadeOffset);
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

        public void DestroyAllCells()
        {
            foreach (var cell in _cells)
            {
                Object.Destroy(cell.CellGameObject);
                foreach (var task in cell.ObjectCreationCoroutines)
                {
                    _temporalLoadBalancer.CancelTask(task);
                }
            }

            _cells.Clear();
        }
    }
}