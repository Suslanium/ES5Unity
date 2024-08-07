using System.Collections;
using System.Linq;
using Engine.Cell.Delegate.Reference;
using Engine.Door;
using Engine.MasterFile;
using MasterFile.MasterFileContents;
using MasterFile.MasterFileContents.Records;
using NIF.Builder;
using UnityEngine;

namespace Engine.Cell.Delegate
{
    public class DoorDelegate : ICellReferencePreprocessDelegate, ICellReferenceInstantiationDelegate
    {
        private readonly NifManager _nifManager;
        private readonly MasterFileManager _masterFileManager;
        private readonly GameEngine _gameEngine;

        public DoorDelegate(NifManager nifManager, MasterFileManager masterFileManager, GameEngine gameEngine)
        {
            _nifManager = nifManager;
            _masterFileManager = masterFileManager;
            _gameEngine = gameEngine;
        }

        public bool IsPreprocessApplicable(CELL cell, LoadCause loadCause, REFR reference, Record referencedRecord)
        {
            return referencedRecord is DOOR door && !string.IsNullOrEmpty(door.NifModelFilename) &&
                   //Currently only teleport doors are loaded because regular doors
                   //will block the location without the ability to open them
                   reference.DoorTeleport != null;
        }

        public IEnumerator PreprocessObject(CELL cell, GameObject cellGameObject, LoadCause loadCause, REFR reference,
            Record referencedRecord)
        {
            if (referencedRecord is not DOOR door)
                yield break;
            _nifManager.PreloadNifFile(door.NifModelFilename);
        }

        public bool IsInstantiationApplicable(CELL cell, LoadCause loadCause, REFR reference, Record referencedRecord)
        {
            return referencedRecord is DOOR &&
                   //Currently only teleport doors are loaded because regular doors
                   //will block the location without the ability to open them
                   reference.DoorTeleport != null;
        }

        public IEnumerator InstantiateObject(CELL cell, GameObject cellGameObject, LoadCause loadCause, REFR reference,
            Record referencedRecord)
        {
            if (referencedRecord is not DOOR door)
                yield break;

            var doorInstantiationCoroutine = InstantiateDoorTeleportAtPositionAndRotation(reference, door,
                reference.Position,
                reference.Rotation, reference.Scale, cellGameObject);
            while (doorInstantiationCoroutine.MoveNext())
                yield return null;
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
                    yield return null;
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

            var cellFormID = _masterFileManager.GetParentFormID(destinationDoorFormID);
            var destinationTask = _masterFileManager.GetFromFormIDTask(cellFormID);
            while (!destinationTask.IsCompleted)
                yield return null;

            if (destinationTask.Result is not CELL destinationCell)
            {
                Debug.LogError($"Destination cell not found for door {doorBase.EditorID}");
                yield break;
            }

            var destinationName = destinationCell.EditorID;
            if ((destinationCell.CellFlag & 0x0001) == 0)
            {
                //TODO Exterior load
                var worldSpaceFormId = _masterFileManager.GetParentFormID(cellFormID);
                var worldSpaceTask = _masterFileManager.GetFromFormIDTask(worldSpaceFormId);
                while (!worldSpaceTask.IsCompleted)
                    yield return null;

                if (worldSpaceTask.Result is not WRLD worldSpace)
                {
                    Debug.LogError(worldSpaceTask.Result == null
                        ? $"Worldspace not found for door {doorBase.EditorID}"
                        : $"Worldspace record is not WRLD for door {doorBase.EditorID} ({worldSpaceTask.Result.Type})");
                }
                else
                {
                    destinationName = worldSpace.EditorID;
                }
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
            doorTeleport.destinationCellName = destinationName;
            doorTeleport.GameEngine = _gameEngine;
            doorTeleport.ChildrenTransforms = childrenSet;

            yield return null;

            CellUtils.ApplyPositionAndRotation(position, rotation, scale, parent, modelObject);
        }
    }
}