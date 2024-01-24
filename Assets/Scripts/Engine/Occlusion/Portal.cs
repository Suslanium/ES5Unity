using UnityEngine;

namespace Engine.Occlusion
{
    public class Portal
    {
        public Room Room1 { get; private set; }
        
        public Room Room2 { get; private set; }
        
        public GameObject PortalObject { get; private set; }
        
        public BoxCollider PortalCollider { get; private set; }
        
        public Portal(Room room1, Room room2, GameObject portalObject, BoxCollider portalCollider)
        {
            Room1 = room1;
            Room2 = room2;
            PortalObject = portalObject;
            PortalCollider = portalCollider;
        }
    }
}