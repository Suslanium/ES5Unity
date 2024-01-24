using System.Collections.Generic;
using UnityEngine;

namespace Engine.Occlusion
{
    public class Room: MonoBehaviour
    {
        public GameObject[] RoomObjects { get; set; }

        public bool IsVisible { get; set; } = true;
        
        public List<Portal> Portals { get; private set; } = new();
        
        public void SetVisibility(bool visible)
        {
            IsVisible = visible;
            foreach (var roomObject in RoomObjects)
            {
                roomObject.SetActive(visible);
            }
        }
    }
}