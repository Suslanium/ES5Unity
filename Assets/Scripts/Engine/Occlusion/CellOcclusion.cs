using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Engine.Occlusion
{
    public class CellOcclusion : MonoBehaviour
    {
        public Dictionary<uint, Room> Rooms { get; set; }

        private Camera _mainCamera;

        private Plane[] _frustumPlanes;

        private Dictionary<uint, Room> CurrentRooms { get; set; } = new();
        
        private readonly Dictionary<(uint, uint), GameObject> _intersectionGameObjects = new();

        private readonly Dictionary<uint, GameObject> _roomGameObjects = new();

        private readonly Dictionary<uint, Room> _currentFrameVisibleRooms = new(30);

        private readonly HashSet<uint> _checkedRooms = new(30);

        private readonly Queue<(uint, Room, Portal)> _roomsToCheck = new(30);

        private int _roomLayer;

        private int _portalLayer;

        private LayerMask _rayCastLayerMask;

        private readonly RaycastHit[] _results = new RaycastHit[30];

        private readonly HashSet<Vector3> _portalHits = new(30);

        private readonly Vector3[] _rayCastDirections = new Vector3[5];

        private readonly float[] _rayCastLengths = new float[5];

        private const float RayCastLengthDelta = 0.5f;

        private Color[] _colors = { Color.red, Color.green, Color.blue, Color.yellow, Color.cyan };

        public void Init(Dictionary<uint, List<GameObject>> roomObjects, GameObject parent)
        {
            _mainCamera = Camera.main;
            Dictionary<uint, List<GameObject>> newRoomObjects = new(roomObjects);
            foreach (var formId in roomObjects.Keys)
            {
                foreach (var formId2 in roomObjects.Keys.Where(formId2 => formId != formId2))
                {
                    if (_intersectionGameObjects.ContainsKey((formId2, formId)))
                    {
                        _intersectionGameObjects.Add((formId, formId2), _intersectionGameObjects[(formId2, formId)]);
                        continue;
                    }

                    var room = roomObjects[formId];
                    var room2 = roomObjects[formId2];
                    var intersectingRoomObjects = room.Intersect(room2).ToList();
                    var intersectionObject = new GameObject($"Intersection {formId} {formId2}");
                    intersectionObject.transform.SetParent(parent.transform, false);
                    foreach (var intersectionGameObject in intersectingRoomObjects)
                    {
                        intersectionGameObject.transform.SetParent(intersectionObject.transform, true);
                    }

                    intersectionObject.SetActive(false);
                    _intersectionGameObjects.Add((formId, formId2), intersectionObject);
                    newRoomObjects[formId] = newRoomObjects[formId].Except(intersectingRoomObjects).ToList();
                    newRoomObjects[formId2] = newRoomObjects[formId2].Except(intersectingRoomObjects).ToList();
                }
            }

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

            foreach (var room in Rooms.Values)
            {
                room.OcclusionObject = this;
            }
        }

        public void Start()
        {
            string[] layerNames = { "Room", "Portal" };
            _roomLayer = LayerMask.NameToLayer(layerNames[0]);
            _portalLayer = LayerMask.NameToLayer(layerNames[1]);
            _rayCastLayerMask = LayerMask.GetMask(layerNames);
        }

        public void AddCurrentRoom(uint formId, Room room)
        {
            CurrentRooms.TryAdd(formId, room);
        }

        public void RemoveCurrentRoom(uint formId, Room room)
        {
            if (!CurrentRooms.ContainsKey(formId)) return;
            CurrentRooms.Remove(formId);
        }

        //TODO the algorithm itself works. It determines the rooms that should be visible and invisible correctly. BUT the whole room activation/deactivation thing is glitchy. Part of the problem is that the Room's IsVisible attribute is inaccurate. But most of the problems here come from the fact that there are intersections between rooms (some objects belong to multiple rooms).
        public void Update()
        {
            _frustumPlanes = GeometryUtility.CalculateFrustumPlanes(_mainCamera);
            foreach (var (formId, room) in CurrentRooms)
            {
                CheckRoomPortals(formId, room);
            }

            _checkedRooms.Clear();

            foreach (var (formId, roomObject) in _roomGameObjects)
            {
                if (CurrentRooms.ContainsKey(formId) || _currentFrameVisibleRooms.ContainsKey(formId))
                    roomObject.SetActive(true);
                else roomObject.SetActive(false);
            }

            foreach (var ((formId, formId2), intersection) in _intersectionGameObjects)
            {
                if (CurrentRooms.ContainsKey(formId) || _currentFrameVisibleRooms.ContainsKey(formId) ||
                    CurrentRooms.ContainsKey(formId2) ||
                    _currentFrameVisibleRooms.ContainsKey(formId2)) intersection.SetActive(true);
                else intersection.SetActive(false);
            }
            _currentFrameVisibleRooms.Clear();
        }

        //TODO this method works, but it doesn't account for rooms without portals
        private void CheckRoomPortals(uint originFormId, Room originRoom)
        {
            _roomsToCheck.Enqueue((originFormId, originRoom, null));
            while (_roomsToCheck.TryDequeue(out var roomToCheck))
            {
                var (formId, room, excludedPortal) = roomToCheck;

                foreach (var portal in room.Portals)
                {
                    if (portal == excludedPortal) continue;

                    var checkedRoom = portal.Room1FormId == formId
                        ? (portal.Room2FormId, portal.Room2)
                        : (portal.Room1FormId, portal.Room1);

                    if (!_checkedRooms.Add(checkedRoom.Item1)) continue;
                    if (CurrentRooms.ContainsKey(checkedRoom.Item1)) continue;

                    var portalCollider = portal.PortalCollider;
                    var bounds = portalCollider.bounds;

                    if (!GeometryUtility.TestPlanesAABB(_frustumPlanes, bounds)) continue;

                    var portalTransform = portalCollider.transform;
                    var portalPosition = portalTransform.position;
                    var cameraPosition = _mainCamera.transform.position;
                    var right = portalTransform.right;
                    var up = portalTransform.up;


                    _rayCastDirections[0] = portalPosition - cameraPosition;
                    _rayCastLengths[0] = Vector3.Distance(portalPosition, cameraPosition) + RayCastLengthDelta;
                    if (bounds.extents.x > 0.5f)
                    {
                        _rayCastDirections[1] = portalPosition + right * bounds.extents.x - cameraPosition;
                        _rayCastDirections[2] = portalPosition - right * bounds.extents.x - cameraPosition;
                        _rayCastLengths[1] =
                            Vector3.Distance(portalPosition + right * bounds.extents.x, cameraPosition) +
                            RayCastLengthDelta;
                        _rayCastLengths[2] =
                            Vector3.Distance(portalPosition - right * bounds.extents.x, cameraPosition) +
                            RayCastLengthDelta;
                    }
                    else
                    {
                        _rayCastDirections[1] = portalPosition + right * bounds.extents.z - cameraPosition;
                        _rayCastDirections[2] = portalPosition - right * bounds.extents.z - cameraPosition;
                        _rayCastLengths[1] =
                            Vector3.Distance(portalPosition + right * bounds.extents.z, cameraPosition) +
                            RayCastLengthDelta;
                        _rayCastLengths[2] =
                            Vector3.Distance(portalPosition - right * bounds.extents.z, cameraPosition) +
                            RayCastLengthDelta;
                    }

                    _rayCastDirections[3] = portalPosition + up * bounds.extents.y - cameraPosition;
                    _rayCastDirections[4] = portalPosition - up * bounds.extents.y - cameraPosition;
                    _rayCastLengths[3] = Vector3.Distance(portalPosition + up * bounds.extents.y, cameraPosition) +
                                         RayCastLengthDelta;
                    _rayCastLengths[4] = Vector3.Distance(portalPosition - up * bounds.extents.y, cameraPosition) +
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
                                _portalHits.Add(RoundVector3(hit.point));
                            }
                        }

                        if (!currentPortalFound) continue;

                        var rayIntersectsRoom = false;
                        for (var i = 0; i < size; i++)
                        {
                            var hit = _results[i];
                            if (hit.transform.gameObject.layer == _portalLayer) continue;
                            if (_portalHits.Contains(RoundVector3(hit.point))) continue;
                            rayIntersectsRoom = true;
                            break;
                        }

                        _portalHits.Clear();

                        if (rayIntersectsRoom) continue;
                        _currentFrameVisibleRooms.Add(checkedRoom.Item1, checkedRoom.Item2);
                        _roomsToCheck.Enqueue((checkedRoom.Item1, checkedRoom.Item2, portal));
                        for (var z = 0; z < 5; z++)
                        {
                            Debug.DrawRay(cameraPosition, _rayCastDirections[z], _colors[z]);
                        }

                        break;
                    }
                }
            }
        }

        private static Vector3 RoundVector3(Vector3 vector3)
        {
            return new Vector3(
                Mathf.Round(vector3.x),
                Mathf.Round(vector3.y),
                Mathf.Round(vector3.z));
        }
    }
}