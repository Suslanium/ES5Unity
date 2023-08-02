using System.Collections;
using System.Collections.Generic;
using System.IO;
using Core;
using MasterFile;
using MasterFile.MasterFileContents;
using MasterFile.MasterFileContents.Records;
using NIF.Converter;
using UnityEngine;

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

        private GameObject InstantiateLightAtPositionAndRotation(REFR refr, LIGH lightRecord, float[] position,
            float[] rotation,
            float scale, GameObject parent)
        {
            GameObject modelObject = null;
            if (!string.IsNullOrEmpty(lightRecord.NifModelFilename))
                modelObject = _nifManager.InstantiateNif(lightRecord.NifModelFilename);
            if (modelObject == null)
                modelObject = new GameObject(lightRecord.EditorID);

            InstantiateLightOnGameObject(refr, lightRecord, modelObject);
            ApplyPositionAndRotation(position, rotation, scale, parent, modelObject);

            return modelObject;
        }

        private void InstantiateLightOnGameObject(REFR reference, LIGH lightRecord, GameObject gameObject)
        {
            if (gameObject != null)
            {
                var light = gameObject.AddComponent<Light>();
                //For some interesting reason the actual radius shown in CK is Base light radius + XRDS value of refr
                light.range = (lightRecord.Radius + reference.Radius) / Convert.meterInMWUnits;
                light.color = new Color32(lightRecord.ColorRGBA[0], lightRecord.ColorRGBA[1], lightRecord.ColorRGBA[2],
                    255);
                //Intensity in Unity != intensity in Skyrim
                light.intensity = 3 * (lightRecord.Fade + reference.FadeOffset);
            }
        }

        private static void ApplyPositionAndRotation(float[] position, float[] rotation, float scale, GameObject parent,
            GameObject modelObject)
        {
            if (modelObject != null)
            {
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