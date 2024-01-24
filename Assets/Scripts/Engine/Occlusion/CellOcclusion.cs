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

        public Dictionary<uint, Room> CurrentRooms { get; private set; } = new();

        public Portal[] Portals { get; set; }

        private readonly Dictionary<(uint, uint), GameObject[]> _intersectingGameObjects = new();

        private static int _roomLayer;

        private static LayerMask _roomLayerMask;

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
            //If the player has a collider - it should also have this layer
            _roomLayer = LayerMask.NameToLayer("Room");
            _roomLayerMask = ~(1 << _roomLayer);
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
        }

        //TODO this thing *kind of* works, but it is extremely inaccurate. The raycast thing should be either greatly improved (that's very unlikely) or replaced with something else. Also there are rooms without portals, they should be handled somehow.
        private void CheckRoomPortals(uint formId, Room room, Portal excludedPortal = null)
        {
            foreach (var portal in room.Portals)
            {
                if (portal == excludedPortal) continue;
                var checkedRoom = portal.Room1FormId == formId
                    ? (portal.Room2FormId, portal.Room2)
                    : (portal.Room1FormId, portal.Room1);
                if (CurrentRooms.ContainsKey(checkedRoom.Item1)) continue;
                var portalCollider = portal.PortalCollider;
                var bounds = portalCollider.bounds;
                if (!GeometryUtility.TestPlanesAABB(_frustumPlanes, bounds))
                {
                    if (!checkedRoom.Item2.IsVisible) continue;
                    DisableRoom(checkedRoom.Item1, checkedRoom.Item2);
                    continue;
                }

                var portalPosition = portalCollider.transform.position;
                var cameraPosition = _mainCamera.transform.position;
                var right = portalCollider.transform.right;
                var up = portalCollider.transform.up;
                var rayDirections = new Vector3[]
                {
                    portalPosition - cameraPosition,
                    portalPosition + right * bounds.extents.x - cameraPosition,
                    portalPosition - right * bounds.extents.x - cameraPosition,
                    portalPosition + up * bounds.extents.y - cameraPosition,
                    portalPosition - up * bounds.extents.y - cameraPosition
                };

                var portalIsVisible = false;
                foreach (var rayDirection in rayDirections)
                {
                    var ray = new Ray(cameraPosition, rayDirection);
                    if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _roomLayerMask)) continue;
                    if (hit.transform != portal.PortalObject.transform) continue;
                    portalIsVisible = true;
                    if (!checkedRoom.Item2.IsVisible)
                        checkedRoom.Item2.SetVisibility(true);
                    CheckRoomPortals(checkedRoom.Item1, checkedRoom.Item2, portal);
                    break;
                }

                if (portalIsVisible) continue;
                if (!checkedRoom.Item2.IsVisible) continue;
                DisableRoom(checkedRoom.Item1, checkedRoom.Item2);
            }
        }
    }
}