using System.Collections;
using System.Collections.Generic;
using System.IO;
using Cinemachine;
using Core;
using Engine.Door;
using Engine.Occlusion;
using MasterFile;
using MasterFile.MasterFileContents;
using MasterFile.MasterFileContents.Records;
using NIF.Builder;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using Convert = Core.Convert;
using Object = UnityEngine.Object;

namespace Engine
{
    public enum LoadCause
    {
        DoorTeleport,
        Coc,
        OpenWorldLoad
    }

    public class CellInfo
    {
        public GameObject CellGameObject { get; set; }

        public List<IEnumerator> ObjectCreationCoroutines { get; } = new();
    }

    public class CellManager
    {
        private readonly GameEngine _gameEngine;
        private readonly ESMasterFile _masterFile;
        private readonly NifManager _nifManager;
        private readonly TemporalLoadBalancer _temporalLoadBalancer;
        private readonly List<CellInfo> _cells = new();
        private readonly GameObject _player;
        private Vector3 _tempPlayerPosition;
        private Quaternion _tempPlayerRotation;
        private static readonly int RoomLayer = LayerMask.NameToLayer("Room");
        private static readonly int PortalLayer = LayerMask.NameToLayer("Portal");
        private readonly List<(GameObject, uint, uint)> _tempPortals = new();
        private readonly Dictionary<uint, GameObject> _tempRooms = new();
        private readonly List<(uint, uint)> _tempLinkedRooms = new();
        private const int DefaultCameraFarPlane = 500;

        public CellManager(ESMasterFile masterFile, NifManager nifManager, TemporalLoadBalancer temporalLoadBalancer,
            GameEngine gameEngine, GameObject player)
        {
            _masterFile = masterFile;
            _nifManager = nifManager;
            _temporalLoadBalancer = temporalLoadBalancer;
            _gameEngine = gameEngine;
            _player = player;
        }

        public void LoadCell(string editorID, bool persistentOnly = false)
        {
            var cellInfo = new CellInfo();
            var creationCoroutine = StartCellLoading(editorID, cellInfo, persistentOnly);
            cellInfo.ObjectCreationCoroutines.Add(creationCoroutine);
            _temporalLoadBalancer.AddTask(creationCoroutine);
            _cells.Add(cellInfo);
        }

        public void LoadCell(uint formID, LoadCause loadCause, Vector3 startPosition, Quaternion startRotation,
            bool persistentOnly = false)
        {
            if (startPosition != Vector3.zero || startRotation != Quaternion.identity)
            {
                _tempPlayerPosition = startPosition;
                _tempPlayerRotation = startRotation;
            }

            var cellInfo = new CellInfo();
            var creationCoroutine = StartCellLoading(formID, cellInfo, loadCause, persistentOnly);
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

            var cellLoadingCoroutine = LoadCellRecord(cell, cellInfo, LoadCause.Coc, persistentOnly);
            while (cellLoadingCoroutine.MoveNext())
            {
                yield return null;
            }
        }

        private IEnumerator StartCellLoading(uint formId, CellInfo cellInfo, LoadCause loadCause,
            bool persistentOnly = false)
        {
            var cellTask = _masterFile.GetFromFormIDTask(formId);
            while (!cellTask.IsCompleted)
            {
                yield return null;
            }

            var cell = (CELL)cellTask.Result;

            var cellLoadingCoroutine = LoadCellRecord(cell, cellInfo, loadCause, persistentOnly);
            while (cellLoadingCoroutine.MoveNext())
            {
                yield return null;
            }
        }

        private IEnumerator LoadCellRecord(CELL cell, CellInfo cellInfo, LoadCause loadCause,
            bool persistentOnly = false)
        {
            //if ((cell.CellFlag & 0x0001) == 0)
            //    throw new InvalidDataException("Trying to load exterior cell as interior");

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

            foreach (var subGroup in childrenGroup.GroupData)
            {
                if (subGroup is not Group group) continue;
                if (group.GroupType != 8 && (group.GroupType != 9 || persistentOnly)) continue;

                var objectInstantiationTask = InstantiateCellReferences(group, cellGameObject, loadCause);
                while (objectInstantiationTask.MoveNext())
                {
                    yield return null;
                }
            }

            var postProcessTask = PostProcessCell(cell, cellGameObject, loadCause != LoadCause.OpenWorldLoad);
            while (postProcessTask.MoveNext())
            {
                yield return null;
            }
        }

        private IEnumerator PostProcessCell(CELL cell, GameObject cellGameObject, bool setPlayerPos)
        {
            cellGameObject.SetActive(true);
            //TODO static batching causes a huge freeze
            StaticBatchingUtility.Combine(cellGameObject);
            yield return null;

            if (_tempPortals.Count > 0 || _tempRooms.Count > 0)
            {
                var cellOcclusion = cellGameObject.AddComponent<CellOcclusion>();
                cellOcclusion.Init(_tempPortals, _tempRooms, _tempLinkedRooms, cellGameObject,
                    _player.GetComponentInChildren<Collider>(), _gameEngine.MainCamera);
            }

            _tempLinkedRooms.Clear();
            _tempPortals.Clear();
            _tempRooms.Clear();

            yield return null;

            if (cell.CellLightingInfo != null)
            {
                var lightingCoroutine = ConfigureCellLighting(cell);
                while (lightingCoroutine.MoveNext())
                {
                    yield return null;
                }
            }
            
            if (setPlayerPos)
            {
                _player.transform.position = _tempPlayerPosition;
                _player.transform.rotation = _tempPlayerRotation;
                _gameEngine.GameState = GameState.InGame;
            }

            _tempPlayerPosition = Vector3.zero;
            _tempPlayerRotation = Quaternion.identity;
            yield return null;
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

        //TODO this thing should be completely rewritten
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

        private IEnumerator InstantiateCellReferences(Group referencesGroup, GameObject parent, LoadCause loadCause)
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
                        if (loadCause == LoadCause.Coc && staticRecord.FormID == 0x32)
                        {
                            _tempPlayerPosition = NifUtils.NifPointToUnityPoint(new Vector3(reference.Position[0],
                                reference.Position[1], reference.Position[2]));
                            _tempPlayerRotation = NifUtils.NifEulerAnglesToUnityQuaternion(
                                new Vector3(reference.Rotation[0], reference.Rotation[1], reference.Rotation[2]));
                            break;
                        }

                        if (staticRecord.FormID == 0x20)
                        {
                            //Portal marker
                            var gameObject = new GameObject("Portal marker")
                            {
                                layer = PortalLayer
                            };
                            var collider = gameObject.AddComponent<BoxCollider>();
                            collider.isTrigger = true;
                            collider.size = NifUtils.NifPointToUnityPoint(reference.Primitive.Bounds) * 2;
                            ApplyPositionAndRotation(reference.Position, reference.Rotation, reference.Scale, parent,
                                gameObject);
                            if (reference.PortalDestinations != null)
                            {
                                _tempPortals.Add((gameObject, reference.PortalDestinations.OriginReference,
                                    reference.PortalDestinations.DestinationReference));
                            }
                            else
                            {
                                gameObject.SetActive(false);
                            }

                            break;
                        }

                        if (staticRecord.FormID == 0x1F)
                        {
                            //Room marker
                            var gameObject = new GameObject("Room marker")
                            {
                                layer = RoomLayer
                            };
                            var collider = gameObject.AddComponent<BoxCollider>();
                            collider.isTrigger = true;
                            collider.size = NifUtils.NifPointToUnityPoint(reference.Primitive.Bounds) * 2;
                            ApplyPositionAndRotation(reference.Position, reference.Rotation, reference.Scale, parent,
                                gameObject);
                            _tempRooms.Add(reference.FormID, gameObject);
                            yield return null;

                            if (reference.LinkedRoomFormIDs.Count > 0)
                            {
                                foreach (var linkedRoomFormID in reference.LinkedRoomFormIDs)
                                {
                                    _tempLinkedRooms.Add((reference.FormID, linkedRoomFormID));
                                }
                            }

                            yield return null;

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
                    case DOOR door:
                        //Currently only teleport doors are loaded because regular doors will block the location without the ability to open them
                        if (!string.IsNullOrEmpty(door.NifModelFilename) && reference.DoorTeleport != null)
                            _nifManager.PreloadNifFile(door.NifModelFilename);
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
                DOOR door => referenceRecord.DoorTeleport != null
                    ? InstantiateDoorTeleportAtPositionAndRotation(referenceRecord, door, referenceRecord.Position,
                        referenceRecord.Rotation, referenceRecord.Scale, parent)
                    : null,
                _ => null
            };
        }

        private IEnumerator InstantiateModelAtPositionAndRotation(string modelPath, float[] position, float[] rotation,
            float scale, GameObject parent)
        {
            var modelObjectCoroutine = _nifManager.InstantiateNif(modelPath,
                modelObject => { ApplyPositionAndRotation(position, rotation, scale, parent, modelObject); });
            while (modelObjectCoroutine.MoveNext())
            {
                yield return null;
            }
        }

        private IEnumerator InstantiateDoorTeleportAtPositionAndRotation(REFR doorRef, DOOR doorBase, float[] position,
            float[] rotation,
            float scale, GameObject parent)
        {
            GameObject modelObject = null;
            if (!string.IsNullOrEmpty(doorBase.NifModelFilename))
            {
                var modelObjectCoroutine =
                    _nifManager.InstantiateNif(doorBase.NifModelFilename, o => { modelObject = o; });
                while (modelObjectCoroutine.MoveNext())
                {
                    yield return null;
                }
            }

            if (modelObject == null)
                modelObject = new GameObject(doorBase.EditorID);

            var doorBounds = new Bounds();
            doorBounds.SetMinMax(
                NifUtils.NifPointToUnityPoint(doorBase.BoundsA),
                NifUtils.NifPointToUnityPoint(doorBase.BoundsB)
            );

            yield return null;

            var collider = modelObject.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.center = doorBounds.center;
            collider.size = doorBounds.size;

            yield return null;

            var destinationDoorFormID = doorBase.RandomTeleports.Count == 0
                ? doorRef.DoorTeleport.DestinationDoorReference
                :
                /*This should be baked into the savefile, but for now it is random every time*/
                doorBase.RandomTeleports[Random.Range(0, doorBase.RandomTeleports.Count)];

            var cellFormID = _masterFile.GetParentFormID(destinationDoorFormID);
            var destinationTask = _masterFile.GetFromFormIDTask(cellFormID);
            while (!destinationTask.IsCompleted)
            {
                yield return null;
            }

            var children = modelObject.GetComponentsInChildren<Transform>();
            yield return null;
            var childrenSet = children.ToHashSet();
            yield return null;

            var teleportPos = NifUtils.NifPointToUnityPoint(new Vector3(doorRef.DoorTeleport.Position[0],
                doorRef.DoorTeleport.Position[1], doorRef.DoorTeleport.Position[2]));
            var teleportRot = NifUtils.NifEulerAnglesToUnityQuaternion(
                new Vector3(doorRef.DoorTeleport.Rotation[0], doorRef.DoorTeleport.Rotation[1],
                    doorRef.DoorTeleport.Rotation[2]));
            var isAutomaticDoor = (doorBase.Flags & 0x02) != 0;

            yield return null;

            var doorTeleport = modelObject.AddComponent<DoorTeleport>();
            doorTeleport.automaticDoor = isAutomaticDoor;
            doorTeleport.teleportPosition = teleportPos;
            doorTeleport.teleportRotation = teleportRot;
            doorTeleport.cellFormID = cellFormID;
            doorTeleport.destinationCellName = ((CELL)destinationTask.Result).EditorID;
            doorTeleport.GameEngine = _gameEngine;
            doorTeleport.ChildrenTransforms = childrenSet;

            yield return null;

            ApplyPositionAndRotation(position, rotation, scale, parent, modelObject);
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

            modelObject.transform.position =
                NifUtils.NifPointToUnityPoint(new Vector3(position[0], position[1], position[2]));
            modelObject.transform.rotation =
                NifUtils.NifEulerAnglesToUnityQuaternion(new Vector3(rotation[0], rotation[1], rotation[2]));
            modelObject.transform.parent = parent.transform;
        }

        public IEnumerator DestroyAllCells()
        {
            var resetLightingCoroutine = ResetLighting();
            while (resetLightingCoroutine.MoveNext())
            {
                yield return null;
            }
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