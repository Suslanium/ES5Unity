using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Engine.Occlusion
{
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

        private readonly Queue<(uint, Room, Portal)> _roomsToCheck = new(30);

        private int _portalLayer;

        private LayerMask _rayCastLayerMask;

        private readonly RaycastHit[] _results = new RaycastHit[30];

        private readonly HashSet<Vector3> _portalHits = new(30);

        private readonly Vector3[] _rayCastDirections = new Vector3[5];

        private readonly float[] _rayCastLengths = new float[5];

        private const float RayCastLengthDelta = 0.5f;

        private const int PlayerColliderSizeMultiplier = 3;

        public void Init(Dictionary<uint, List<GameObject>> roomObjects, GameObject parent, List<Room> rooms, Collider playerCollider)
        {
            _playerCollider = playerCollider;
            _mainCamera = Camera.main;
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
                        _roomIntersectingObjects.Add(formId, new List<GameObject>{ child.gameObject });
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

        public void Start()
        {
            string[] layerNames = { "Room", "Portal" };
            LayerMask.NameToLayer(layerNames[0]);
            _portalLayer = LayerMask.NameToLayer(layerNames[1]);
            _rayCastLayerMask = LayerMask.GetMask(layerNames);
        }

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

        //TODO the algorithm itself works. Also, the algorithm itself is efficient enough, however the room activation/deactivation should be optimized if possible
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

                    if (_currentFrameVisibleRooms.Contains(checkedRoom.Item1)) continue;
                    if (_currentRooms.ContainsKey(checkedRoom.Item1)) continue;

                    var portalCollider = portal.PortalCollider;
                    
                    if (portalCollider.bounds.Intersects(playerColliderBounds))
                    {
                        _currentFrameVisibleRooms.Add(checkedRoom.Item1);
                        _roomsToCheck.Enqueue((checkedRoom.Item1, checkedRoom.Item2, portal)); 
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
                                _portalHits.Add(FloorVector3(hit.point));
                            }
                        }

                        if (!currentPortalFound) continue;

                        var rayIntersectsRoom = false;
                        for (var i = 0; i < size; i++)
                        {
                            var hit = _results[i];
                            if (hit.transform.gameObject.layer == _portalLayer) continue;
                            if (_portalHits.Contains(FloorVector3(hit.point))) continue;
                            if (playerColliderBounds.Contains(hit.point)) continue;
                            rayIntersectsRoom = true;
                            break;
                        }

                        _portalHits.Clear();

                        if (rayIntersectsRoom) continue;
                        _currentFrameVisibleRooms.Add(checkedRoom.Item1);
                        _roomsToCheck.Enqueue((checkedRoom.Item1, checkedRoom.Item2, portal));

                        break;
                    }
                }
            }
        }

        //TODO this is a bit inaccurate: sometimes Floor/Ceil works better, sometimes Round works better
        private static Vector3 FloorVector3(Vector3 vector3)
        {
            return new Vector3(
                Mathf.Round(vector3.x),
                Mathf.Round(vector3.y),
                Mathf.Round(vector3.z));
        }
    }
}