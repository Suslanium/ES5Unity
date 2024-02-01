﻿using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Engine.Occlusion
{
    /// <summary>
    /// This script was supposed to do runtime occlusion culling based on the cell's room and portal markers.
    /// In an ideal world where every room was connected to the others with a portal, it would work.
    /// But this is Skyrim. There's a lot of rooms that are connected to each other WITHOUT portals. There's a lot of intersecting rooms.
    /// Creating a script that takes into account all of these 'exceptional' cases AND displays ONLY visible rooms seems near impossible to me.
    /// Let's say you have two rooms with a hallway between them. But there is no portal in that hallway. How do you determine that these rooms are connected?
    /// You can check if they intersect each other. But what if they don't intersect? What if these rooms just 'touch' each other on some edge?
    /// You can't say for sure that a room is connected to another one only with this information.
    /// Honestly I just gave up on doing this thing. I'd rather write my own custom occlusion culling than try to adapt stuff from Skyrim.
    /// The commented part of the script is the unfinished part that should've made this thing work with all the 'exceptional' cases.
    /// </summary>
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

        private readonly Queue<(uint, Room, Portal/*, HashSet<GameObject>*/)> _roomsToCheck = new(30);

        private static int _portalLayer;

        private static int _roomLayer;

        private LayerMask _rayCastLayerMask;

        //private LayerMask _roomLayerMask;

        private readonly RaycastHit[] _results = new RaycastHit[30];

        private readonly HashSet<Vector3> _portalHits = new(30);

        private readonly Vector3[] _rayCastDirections = new Vector3[5];

        private readonly float[] _rayCastLengths = new float[5];

        private const float RayCastLengthDelta = 0.5f;

        private const int PlayerColliderSizeMultiplier = 3;

        //private readonly Vector3 _roomIntersectionThreshold = Vector3.one * 1f;

        #region initialization

        public void Init(List<(GameObject, uint, uint)> portals, Dictionary<uint, GameObject> roomObject,
            GameObject cellGameObject, Collider playerCollider)
        {
            _portalLayer = LayerMask.NameToLayer("Portal");
            _roomLayer = LayerMask.NameToLayer("Room");
            _rayCastLayerMask = LayerMask.GetMask("Room", "Portal");
            //_roomLayerMask = LayerMask.GetMask("Room");
            _playerCollider = playerCollider;
            _mainCamera = Camera.main;
            var rooms = new Dictionary<uint, Room>();
            var roomObjects = new Dictionary<uint, List<GameObject>>();
            //var portalFormIds = new HashSet<(uint, uint)>(portals.Count);
            foreach (var (portalObject, originFormID, destinationFormID) in portals)
            {
                if (!roomObject.ContainsKey(originFormID) || !roomObject.ContainsKey(destinationFormID)) continue;
                var originRoom = roomObject[originFormID];
                var destinationRoom = roomObject[destinationFormID];
                var originRoomInstance = rooms.GetValueOrDefault(originFormID);
                if (originRoomInstance == null)
                {
                    originRoomInstance = originRoom.AddComponent<Room>();
                    roomObjects.Add(originFormID,
                        GetRoomGameObjects(cellGameObject, originRoom.GetComponent<BoxCollider>()));
                    rooms.Add(originFormID, originRoomInstance);
                    originRoomInstance.FormId = originFormID;
                }

                var destinationRoomInstance = rooms.GetValueOrDefault(destinationFormID);
                if (destinationRoomInstance == null)
                {
                    destinationRoomInstance = destinationRoom.AddComponent<Room>();
                    roomObjects.Add(destinationFormID,
                        GetRoomGameObjects(cellGameObject, destinationRoom.GetComponent<BoxCollider>()));
                    rooms.Add(destinationFormID, destinationRoomInstance);
                    destinationRoomInstance.FormId = destinationFormID;
                }

                var portal = new Portal(originRoomInstance, destinationRoomInstance, originFormID,
                    destinationFormID,
                    portalObject,
                    portalObject.GetComponent<BoxCollider>());
                //portalFormIds.Add((originFormID, destinationFormID));
                originRoomInstance.Portals.Add(portal);
                destinationRoomInstance.Portals.Add(portal);
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

            // foreach (var (formId, room) in rooms)
            // {
            //     var roomTransform = room.transform;
            //     var roomTrigger = room.GetComponent<BoxCollider>();
            //     var overlappingRooms = Physics.OverlapBox(roomTransform.position,
            //         roomTrigger.size / 2 - _roomIntersectionThreshold,
            //         roomTransform.rotation, _roomLayerMask);
            //     foreach (var overlappingRoom in overlappingRooms)
            //     {
            //         if (room.gameObject == overlappingRoom.gameObject) continue;
            //         var roomInstance = overlappingRoom.GetComponent<Room>();
            //         if (roomInstance == null) continue;
            //         if (portalFormIds.Contains((formId, roomInstance.FormId)) ||
            //             portalFormIds.Contains((roomInstance.FormId, formId))) continue;
            //         room.NonPortalConnections.TryAdd(roomInstance.FormId, roomInstance);
            //     }
            //
            //     if (room.Portals.Count > 1) continue;
            //     var additionalOverlapCheck = Physics.OverlapBox(roomTransform.position,
            //         roomTrigger.size / 2,
            //         roomTransform.rotation, _roomLayerMask).Except(overlappingRooms);
            //     foreach (var edgeRoom in additionalOverlapCheck)
            //     {
            //         if (room.gameObject == edgeRoom.gameObject) continue;
            //         var roomInstance = edgeRoom.GetComponent<Room>();
            //         if (roomInstance == null) continue;
            //         if (roomInstance.Portals.Count > 1 && room.Portals.Count > 0) continue;
            //         if (portalFormIds.Contains((formId, roomInstance.FormId)) ||
            //             portalFormIds.Contains((roomInstance.FormId, formId))) continue;
            //         room.NonPortalConnections.TryAdd(roomInstance.FormId, roomInstance);
            //     }
            // }

            PreProcessRoomIntersections(roomObjects, cellGameObject, rooms.Values.ToList());
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
                    if (!_roomIntersectingObjects.ContainsKey(formId))
                        _roomIntersectingObjects.Add(formId, new List<GameObject> { child.gameObject });
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

        //TODO this thing works, but it doesn't account for rooms without portals
        private void CheckRoomPortals(uint originFormId, Room originRoom)
        {
            var playerColliderBounds = _playerCollider.bounds;
            playerColliderBounds.Expand(Vector3.one * PlayerColliderSizeMultiplier);
            _roomsToCheck.Enqueue((originFormId, originRoom, null/*, null*/));
            while (_roomsToCheck.TryDequeue(out var roomToCheck))
            {
                var (formId, room, excludedPortal/*, ignoredRoomColliders*/) = roomToCheck;

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
                        _roomsToCheck.Enqueue((checkedRoom.Item1, checkedRoom.Item2, portal/*, ignoredRoomColliders*/));
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

                    for (var j = 0; j < 5; j++)
                    {
                        var size = Physics.RaycastNonAlloc(cameraPosition, _rayCastDirections[j], _results,
                            _rayCastLengths[j], _rayCastLayerMask);

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
                            //if (ignoredRoomColliders != null && ignoredRoomColliders.Contains(hit.transform.gameObject))
                                //continue;
                            if (playerColliderBounds.Contains(hit.point)) continue;
                            rayIntersectsRoom = true;
                            break;
                        }

                        _portalHits.Clear();

                        if (rayIntersectsRoom) continue;
                        Debug.DrawRay(cameraPosition, _rayCastDirections[j], Color.red);
                        _currentFrameVisibleRooms.Add(checkedRoom.Item1);
                        _roomsToCheck.Enqueue((checkedRoom.Item1, checkedRoom.Item2, portal/*, ignoredRoomColliders*/));

                        break;
                    }
                }

                // foreach (var (roomFormId, nonConnectedRoom) in room.NonPortalConnections)
                // {
                //     if (_currentFrameVisibleRooms.Contains(roomFormId)) continue;
                //     if (_currentRooms.ContainsKey(roomFormId)) continue;
                //
                //     if (!GeometryUtility.TestPlanesAABB(_frustumPlanes, nonConnectedRoom.RoomTrigger.bounds)) continue;
                //
                //     if (ignoredRoomColliders == null)
                //     {
                //         _currentFrameVisibleRooms.Add(roomFormId);
                //         _roomsToCheck.Enqueue((roomFormId, nonConnectedRoom, null,
                //             new HashSet<GameObject> { nonConnectedRoom.gameObject }));
                //     }
                //     else
                //     {
                //         _currentFrameVisibleRooms.Add(roomFormId);
                //         _roomsToCheck.Enqueue((roomFormId, nonConnectedRoom, null,
                //             new HashSet<GameObject>(ignoredRoomColliders) { nonConnectedRoom.gameObject }));
                //     }
                // }
            }
        }

        private static Vector3 FloorVector3ToEven(Vector3 vector3)
        {
            return new Vector3(
                Mathf.Floor(vector3.x / 2) * 2,
                Mathf.Floor(vector3.y / 2) * 2,
                Mathf.Floor(vector3.z / 2) * 2);
        }

        #endregion
    }
}