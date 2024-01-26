using System;
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

        public Portal[] Portals { get; set; }

        private readonly Dictionary<(uint, uint), GameObject[]> _intersectingGameObjects = new();

        private readonly Dictionary<uint, Room> _currentFrameVisibleRooms = new(30);

        private readonly Dictionary<uint, Room> _previousFrameVisibleRooms = new(30);

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

        public void Init()
        {
            foreach (var room in Rooms.Values)
            {
                room.OcclusionObject = this;
                room.SetVisibility(false);
            }

            _mainCamera = Camera.main;

            foreach (var (formId, room) in Rooms)
            {
                foreach (var (formId2, room2) in Rooms)
                {
                    if (formId == formId2) continue;
                    if (_intersectingGameObjects.ContainsKey((formId2, formId)))
                    {
                        _intersectingGameObjects.Add((formId, formId2), _intersectingGameObjects[(formId2, formId)]);
                        continue;
                    }

                    var intersectingGameObjects = room.RoomObjects.Intersect(room2.RoomObjects).ToArray();
                    _intersectingGameObjects.Add((formId, formId2), intersectingGameObjects);
                }
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
            if (!CurrentRooms.TryAdd(formId, room)) return;
            if (room.IsVisible) return;
            room.SetVisibility(true);
        }

        public void RemoveCurrentRoom(uint formId, Room room)
        {
            if (!CurrentRooms.ContainsKey(formId)) return;
            CurrentRooms.Remove(formId);
            if (!room.IsVisible) return;
            DisableRoom(formId, room);
        }

        private void DisableRoom(uint formId, Room room)
        {
            room.SetVisibility(false);
            foreach (var intersection in CurrentRooms.Keys.SelectMany(visibleFormId =>
                         visibleFormId != formId
                             ? _intersectingGameObjects[(formId, visibleFormId)]
                             : Array.Empty<GameObject>()))
            {
                intersection.SetActive(true);
            }
        }

        public void Update()
        {
            _frustumPlanes = GeometryUtility.CalculateFrustumPlanes(_mainCamera);
            foreach (var (formId, room) in CurrentRooms)
            {
                CheckRoomPortals(formId, room);
            }

            _checkedRooms.Clear();

            foreach (var (formId, room) in _previousFrameVisibleRooms)
            {
                if (_currentFrameVisibleRooms.ContainsKey(formId)) continue;
                if (CurrentRooms.ContainsKey(formId)) continue;
                if (room.IsVisible) 
                    DisableRoom(formId, room);
            }

            _previousFrameVisibleRooms.Clear();
            foreach (var (formId, room) in _currentFrameVisibleRooms)
            {
                _previousFrameVisibleRooms.Add(formId, room);
                //if (!room.IsVisible) 
                    room.SetVisibility(true);
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
                    
                    if (!GeometryUtility.TestPlanesAABB(_frustumPlanes, bounds))
                    {
                        // if (!checkedRoom.Item2.IsVisible) continue;
                        // DisableRoom(checkedRoom.Item1, checkedRoom.Item2);
                        continue;
                    }

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

                    //var portalIsVisible = false;
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
                        // portalIsVisible = true;
                        //  if (!checkedRoom.Item2.IsVisible)
                        //      checkedRoom.Item2.SetVisibility(true);
                        _currentFrameVisibleRooms.Add(checkedRoom.Item1, checkedRoom.Item2);
                        _roomsToCheck.Enqueue((checkedRoom.Item1, checkedRoom.Item2, portal));
                        for (var z = 0; z < 5; z++)
                        {
                            Debug.DrawRay(cameraPosition, _rayCastDirections[z], _colors[z]);
                        }

                        //CheckRoomPortals(checkedRoom.Item1, checkedRoom.Item2, portal);
                        break;
                    }

                    // if (portalIsVisible) continue;
                    //  if (!checkedRoom.Item2.IsVisible) continue;
                    //  DisableRoom(checkedRoom.Item1, checkedRoom.Item2);
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