using UnityEngine;

namespace Engine.Occlusion
{
    public class Portal
    {
        public uint Room1FormId { get; private set; }
        
        public Room Room1 { get; private set; }
        
        public uint Room2FormId { get; private set; }
        
        public Room Room2 { get; private set; }
        
        public GameObject PortalObject { get; private set; }
        
        public BoxCollider PortalCollider { get; private set; }
        
        public Portal(Room room1, Room room2, uint room1FormId, uint room2FormId, GameObject portalObject, BoxCollider portalCollider)
        {
            Room1FormId = room1FormId;
            Room2FormId = room2FormId;
            Room1 = room1;
            Room2 = room2;
            PortalObject = portalObject;
            PortalCollider = portalCollider;
        }
    }
}