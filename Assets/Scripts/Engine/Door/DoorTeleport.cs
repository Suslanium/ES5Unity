using System.Collections.Generic;
using UnityEngine;

namespace Engine.Door
{
    public class DoorTeleport : MonoBehaviour
    {
        public uint cellFormID;

        public Vector3 teleportPosition;

        public Quaternion teleportRotation;

        public string destinationCellName;

        public bool automaticDoor;

        public GameEngine GameEngine;

        private BoxCollider _doorTrigger;

        private const float MinDoorDistance = 3f;

        public HashSet<Transform> ChildrenTransforms;

        private void Start()
        {
            _doorTrigger = GetComponent<BoxCollider>();
        }

        private void Update()
        {
            if (automaticDoor) return;
            if (!(Vector3.Distance(GameEngine.MainCamera.transform.position, transform.position) < MinDoorDistance) ||
                !GeometryUtility.TestPlanesAABB(GameEngine.CameraPlanes, _doorTrigger.bounds))
            {
                if (GameEngine.ActiveDoorTeleport == this) GameEngine.ActiveDoorTeleport = null;
                return;
            }
            
            var cameraTransform = GameEngine.MainCamera.transform;
            if (!Physics.Raycast(cameraTransform.position, cameraTransform.forward, out var rayCastHit,
                    MinDoorDistance))
            {
                if (GameEngine.ActiveDoorTeleport == this) GameEngine.ActiveDoorTeleport = null;
                return;
            }
            
            if (rayCastHit.transform == transform || ChildrenTransforms.Contains(rayCastHit.transform))
            {
                GameEngine.ActiveDoorTeleport = this;
            }
            else
            {
                if (GameEngine.ActiveDoorTeleport == this) GameEngine.ActiveDoorTeleport = null;
            }
        }
    }
}