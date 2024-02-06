using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Engine.Occlusion
{
    /*TODO this works, but there are some problems:
     The method that rounds vectors sometimes is not really accurate (this results in some rooms being invisible)
     The raycast sometimes doesn't get to the portal through another portal even though it is clearly visible (this happens mostly when the first portal is small and the other one is huge)
     Mostly likely this script will be later replaced because the problems above are pretty hard to fix while keeping this thing efficient
     */
    public class CellOcclusion : MonoBehaviour
    {
        private Camera _mainCamera;

        private Collider _playerCollider;

        private Plane[] _frustumPlanes;

        private readonly Dictionary<uint, Room> _currentRooms = new();

        private bool _currentRoomSetHasChanged;

        private readonly Dictionary<uint, List<GameObject>> _roomIntersectingObjects = new();

        private readonly Dictionary<GameObject, bool> _intersectionObjectShouldBeActive = new();

        private List<GameObject> _interSectionObjectKeys;

        private readonly Dictionary<uint, GameObject> _roomGameObjects = new();

        private readonly HashSet<uint> _currentFrameVisibleRooms = new(30);

        private readonly HashSet<uint> _previousFrameVisibleRooms = new(30);

        private readonly Queue<(uint, Room, Portal, Vector3)> _roomsToCheck = new(30);

        private static int _portalLayer;

        private static int _roomLayer;

        private LayerMask _rayCastLayerMask;

        private readonly RaycastHit[] _results = new RaycastHit[30];

        private readonly HashSet<Vector3> _portalHits = new(30);

        private readonly Vector3[] _rayCastDirections = new Vector3[5];

        private readonly float[] _rayCastLengths = new float[5];

        private const float RayCastLengthDelta = 0.5f;

        private const int PlayerColliderSizeMultiplier = 3;

        #region initialization

        public void Init(List<(GameObject, uint, uint)> portals, Dictionary<uint, GameObject> roomObject,
            List<(uint, uint)> linkedRooms,
            GameObject cellGameObject, Collider playerCollider)
        {
            _portalLayer = LayerMask.NameToLayer("Portal");
            _roomLayer = LayerMask.NameToLayer("Room");
            _rayCastLayerMask = LayerMask.GetMask("Room", "Portal");
            _playerCollider = playerCollider;
            _mainCamera = Camera.main;
            var rooms = new Dictionary<uint, Room>();
            var roomObjects = new Dictionary<uint, List<GameObject>>();
            var portalFormIds = new HashSet<(uint, uint)>(portals.Count);
            foreach (var (portalObject, originFormID, destinationFormID) in portals)
            {
                var (processingSuccess, originRoomInstance, destinationRoomInstance) = ProcessRoomPair(roomObject,
                    roomObjects, rooms, cellGameObject, originFormID, destinationFormID);
                if (!processingSuccess) continue;

                var portal = new Portal(originRoomInstance, destinationRoomInstance, originFormID,
                    destinationFormID,
                    portalObject,
                    portalObject.GetComponent<BoxCollider>());
                portalFormIds.Add((originFormID, destinationFormID));
                originRoomInstance.Portals.Add(portal);
                destinationRoomInstance.Portals.Add(portal);
            }

            foreach (var (firstRoom, secondRoom) in linkedRooms)
            {
                if (portalFormIds.Contains((firstRoom, secondRoom)) ||
                    portalFormIds.Contains((secondRoom, firstRoom))) continue;
                var (processingSuccess, firstRoomInstance, secondRoomInstance) = ProcessRoomPair(roomObject,
                    roomObjects, rooms, cellGameObject, firstRoom, secondRoom);
                if (!processingSuccess) continue;

                var firstRoomBounds = firstRoomInstance.GetComponent<BoxCollider>().bounds;
                var secondRoomBounds = secondRoomInstance.GetComponent<BoxCollider>().bounds;

                var portalBounds = new Bounds();
                portalBounds.SetMinMax(
                    Vector3.Max(firstRoomBounds.min, secondRoomBounds.min),
                    Vector3.Min(firstRoomBounds.max, secondRoomBounds.max)
                );

                var portalGameObject = new GameObject("Generated portal marker")
                {
                    layer = _portalLayer,
                    transform =
                    {
                        position = portalBounds.center
                    }
                };
                var portalCollider = portalGameObject.AddComponent<BoxCollider>();
                portalCollider.isTrigger = true;
                portalCollider.size = portalBounds.size;
                portalGameObject.transform.SetParent(cellGameObject.transform, true);
                var portal = new Portal(firstRoomInstance, secondRoomInstance, firstRoom, secondRoom, portalGameObject,
                    portalCollider);
                portalFormIds.Add((firstRoom, secondRoom));
                firstRoomInstance.Portals.Add(portal);
                secondRoomInstance.Portals.Add(portal);
            }

            foreach (var roomWithoutPortalsFormId in roomObject.Keys.Except(rooms.Keys))
            {
                var room = roomObject[roomWithoutPortalsFormId];
                var roomInstance = room.AddComponent<Room>();
                roomObjects.Add(roomWithoutPortalsFormId,
                    GetRoomGameObjects(cellGameObject, room.GetComponent<BoxCollider>()));
                roomInstance.FormId = roomWithoutPortalsFormId;
                rooms.Add(roomWithoutPortalsFormId, roomInstance);
            }

            PreProcessRoomIntersections(roomObjects, cellGameObject, rooms.Values.ToList());
        }

        private static (bool, Room, Room) ProcessRoomPair(IReadOnlyDictionary<uint, GameObject> roomObject,
            IDictionary<uint, List<GameObject>> roomObjects, Dictionary<uint, Room> rooms, GameObject cellGameObject,
            uint roomId1, uint roomId2)
        {
            if (!roomObject.ContainsKey(roomId1) || !roomObject.ContainsKey(roomId2)) return (false, null, null);
            var originRoom = roomObject[roomId1];
            var destinationRoom = roomObject[roomId2];
            var originRoomInstance = rooms.GetValueOrDefault(roomId1);
            if (originRoomInstance == null)
            {
                originRoomInstance = originRoom.AddComponent<Room>();
                roomObjects.Add(roomId1,
                    GetRoomGameObjects(cellGameObject, originRoom.GetComponent<BoxCollider>()));
                rooms.Add(roomId1, originRoomInstance);
                originRoomInstance.FormId = roomId1;
            }

            var destinationRoomInstance = rooms.GetValueOrDefault(roomId2);
            if (destinationRoomInstance != null) return (true, originRoomInstance, destinationRoomInstance);

            destinationRoomInstance = destinationRoom.AddComponent<Room>();
            roomObjects.Add(roomId2,
                GetRoomGameObjects(cellGameObject, destinationRoom.GetComponent<BoxCollider>()));
            rooms.Add(roomId2, destinationRoomInstance);
            destinationRoomInstance.FormId = roomId2;

            return (true, originRoomInstance, destinationRoomInstance);
        }

        private static List<GameObject> GetRoomGameObjects(GameObject cellGameObject, BoxCollider roomTrigger)
        {
            var roomSize = roomTrigger.size;
            var localBounds = new Bounds(Vector3.zero, roomSize);
            var roomTransform = roomTrigger.transform;
            var colliders = Physics.OverlapBox(roomTransform.position, roomSize / 2, roomTransform.rotation);
            var childrenInCollider = colliders.Where(collider =>
                {
                    GameObject gameObject;
                    return (gameObject = collider.gameObject).layer != _roomLayer
                           && gameObject.layer != _portalLayer
                           && gameObject.transform.IsChildOf(cellGameObject.transform);
                }).Select(collider => GetDirectChild(collider.gameObject, cellGameObject))
                .Where(directChild => directChild != null).ToList();

            childrenInCollider.AddRange(
                from Transform child
                    in cellGameObject.transform
                where localBounds.Contains(roomTrigger.transform.InverseTransformPoint(child.transform.position))
                      && child.gameObject.layer != _roomLayer
                      && child.gameObject.layer != _portalLayer
                      && child.GetComponent<Light>() == null
                select child.gameObject);

            return childrenInCollider.Distinct().ToList();
        }

        private static GameObject GetDirectChild(GameObject nestedChild, GameObject parent)
        {
            var currentParent = nestedChild.transform.parent;

            while (currentParent != null)
            {
                if (currentParent.parent == parent.transform)
                {
                    return currentParent.gameObject;
                }

                currentParent = currentParent.parent;
            }

            return null;
        }

        private void PreProcessRoomIntersections(Dictionary<uint, List<GameObject>> roomObjects, GameObject parent,
            List<Room> rooms)
        {
            Dictionary<uint, List<GameObject>> newRoomObjects = new(roomObjects);
            
            foreach (var room in rooms)
            {
                _roomIntersectingObjects.Add(room.FormId, new List<GameObject>());
            }
            
            foreach (Transform child in parent.transform)
            {
                List<uint> roomIds = new();
                foreach (var (formId, currentRoom) in roomObjects)
                {
                    if (currentRoom.Contains(child.gameObject))
                        roomIds.Add(formId);
                }

                if (roomIds.Count <= 1) continue;

                foreach (var formId in roomIds)
                {
                    newRoomObjects[formId].Remove(child.gameObject);
                    _roomIntersectingObjects[formId].Add(child.gameObject);
                }

                _intersectionObjectShouldBeActive.Add(child.gameObject, false);
                child.gameObject.SetActive(false);
            }

            _interSectionObjectKeys = _intersectionObjectShouldBeActive.Keys.ToList();

            foreach (var (formId, currentRoomObjects) in newRoomObjects)
            {
                var roomObject = new GameObject($"Room {formId}");
                roomObject.transform.SetParent(parent.transform, false);
                foreach (var currentRoomObject in currentRoomObjects)
                {
                    currentRoomObject.transform.SetParent(roomObject.transform, true);
                }

                roomObject.SetActive(false);
                _roomGameObjects.Add(formId, roomObject);
            }

            foreach (var room in rooms)
            {
                room.OcclusionObject = this;
            }
        }

        #endregion

        #region processing

        public void AddCurrentRoom(uint formId, Room room)
        {
            _currentRooms.TryAdd(formId, room);
            _currentRoomSetHasChanged = true;
        }

        public void RemoveCurrentRoom(uint formId)
        {
            if (!_currentRooms.ContainsKey(formId)) return;
            _currentRooms.Remove(formId);
            _currentRoomSetHasChanged = true;
        }

        //TODO the algorithm itself is efficient enough, however the room activation/deactivation should be optimized if possible
        public void FixedUpdate()
        {
            _frustumPlanes = GeometryUtility.CalculateFrustumPlanes(_mainCamera);
            foreach (var (formId, room) in _currentRooms)
            {
                CheckRoomPortals(formId, room);
            }

            if (!_currentRoomSetHasChanged && _currentFrameVisibleRooms.SetEquals(_previousFrameVisibleRooms))
            {
                _currentFrameVisibleRooms.Clear();
                return;
            }

            _previousFrameVisibleRooms.Clear();

            foreach (var (formId, roomObject) in _roomGameObjects)
            {
                if (_currentRooms.ContainsKey(formId) || _currentFrameVisibleRooms.Contains(formId))
                {
                    if (_currentFrameVisibleRooms.Contains(formId))
                        _previousFrameVisibleRooms.Add(formId);
                    roomObject.SetActive(true);
                    foreach (var intersectionObject in _roomIntersectingObjects[formId])
                    {
                        _intersectionObjectShouldBeActive[intersectionObject] = true;
                    }
                }
                else roomObject.SetActive(false);
            }

            foreach (var intersectionObject in _interSectionObjectKeys)
            {
                intersectionObject.SetActive(_intersectionObjectShouldBeActive[intersectionObject]);
                _intersectionObjectShouldBeActive[intersectionObject] = false;
            }

            if (_currentRoomSetHasChanged)
                _currentRoomSetHasChanged = false;

            _currentFrameVisibleRooms.Clear();
        }

        private void CheckRoomPortals(uint originFormId, Room originRoom)
        {
            var playerColliderBounds = _playerCollider.bounds;
            playerColliderBounds.Expand(Vector3.one * PlayerColliderSizeMultiplier);
            _roomsToCheck.Enqueue((originFormId, originRoom, null, Vector3.zero));
            while (_roomsToCheck.TryDequeue(out var roomToCheck))
            {
                var (formId, room, excludedPortal, previousRayDirection) = roomToCheck;

                foreach (var portal in room.Portals)
                {
                    if (portal == excludedPortal) continue;

                    var checkedRoom = portal.Room1FormId == formId
                        ? (portal.Room2FormId, portal.Room2)
                        : (portal.Room1FormId, portal.Room1);

                    if (_currentFrameVisibleRooms.Contains(checkedRoom.Item1)) continue;
                    if (_currentRooms.ContainsKey(checkedRoom.Item1)) continue;

                    var portalCollider = portal.PortalCollider;

                    if (portalCollider.bounds.Intersects(playerColliderBounds))
                    {
                        _currentFrameVisibleRooms.Add(checkedRoom.Item1);
                        _roomsToCheck.Enqueue((checkedRoom.Item1, checkedRoom.Item2,
                            portal, Vector3.zero));
                        continue;
                    }

                    if (!GeometryUtility.TestPlanesAABB(_frustumPlanes, portalCollider.bounds)) continue;

                    var portalTransform = portalCollider.transform;
                    var portalPosition = portalTransform.position;
                    var cameraPosition = _mainCamera.transform.position;
                    var right = portalTransform.right;
                    var up = portalTransform.up;
                    var portalSize = portalCollider.size;


                    _rayCastDirections[0] = portalPosition - cameraPosition;
                    _rayCastLengths[0] = Vector3.Distance(portalPosition, cameraPosition) + RayCastLengthDelta;
                    _rayCastDirections[1] = portalPosition + right * portalSize.x / 2 - cameraPosition;
                    _rayCastDirections[2] = portalPosition - right * portalSize.x / 2 - cameraPosition;
                    _rayCastLengths[1] =
                        Vector3.Distance(portalPosition + right * portalSize.x / 2, cameraPosition) +
                        RayCastLengthDelta;
                    _rayCastLengths[2] =
                        Vector3.Distance(portalPosition - right * portalSize.x / 2, cameraPosition) +
                        RayCastLengthDelta;
                    _rayCastDirections[3] = portalPosition + up * portalSize.y / 2 - cameraPosition;
                    _rayCastDirections[4] = portalPosition - up * portalSize.y / 2 - cameraPosition;
                    _rayCastLengths[3] = Vector3.Distance(portalPosition + up * portalSize.y / 2, cameraPosition) +
                                         RayCastLengthDelta;
                    _rayCastLengths[4] = Vector3.Distance(portalPosition - up * portalSize.y / 2, cameraPosition) +
                                         RayCastLengthDelta;

                    var distance = 0f;
                    for (var j = 0; j < 6; j++)
                    {
                        if (j == 5 && !portal.PortalCollider.bounds.IntersectRay(
                                new Ray(cameraPosition, previousRayDirection), out distance)) break;
                        var size = j < 5
                            ? Physics.RaycastNonAlloc(cameraPosition, _rayCastDirections[j], _results,
                                _rayCastLengths[j], _rayCastLayerMask)
                            : Physics.RaycastNonAlloc(cameraPosition, previousRayDirection, _results,
                                distance + RayCastLengthDelta, _rayCastLayerMask);

                        var currentPortalFound = false;
                        for (var i = 0; i < size; i++)
                        {
                            var hit = _results[i];
                            if (!currentPortalFound && hit.transform == portal.PortalObject.transform)
                            {
                                currentPortalFound = true;
                            }

                            if (hit.transform.gameObject.layer == _portalLayer)
                            {
                                _portalHits.Add(FloorVector3ToEven(hit.point));
                            }
                        }

                        if (!currentPortalFound) continue;

                        var rayIntersectsRoom = false;
                        for (var i = 0; i < size; i++)
                        {
                            var hit = _results[i];
                            if (hit.transform.gameObject.layer == _portalLayer) continue;
                            if (_portalHits.Contains(FloorVector3ToEven(hit.point))) continue;
                            if (playerColliderBounds.Contains(hit.point)) continue;
                            rayIntersectsRoom = true;
                            break;
                        }

                        _portalHits.Clear();

                        if (rayIntersectsRoom) continue;
                        _currentFrameVisibleRooms.Add(checkedRoom.Item1);
                        _roomsToCheck.Enqueue((checkedRoom.Item1, checkedRoom.Item2,
                            portal, j < 5 ? _rayCastDirections[j] : previousRayDirection));

                        break;
                    }
                }
            }
        }

        private static Vector3 FloorVector3ToEven(Vector3 vector3)
        {
            return new Vector3(
                Mathf.Round(vector3.x / 6) * 6,
                Mathf.Round(vector3.y / 6) * 6,
                Mathf.Round(vector3.z / 6) * 6);
        }

        #endregion
    }
}