using System.Collections.Generic;
using UnityEngine;

namespace Engine.Occlusion
{
    public class Room: MonoBehaviour
    {
        public uint FormId { get; set; }
        
        public GameObject[] RoomObjects { get; set; }

        public bool IsVisible { get; set; } = true;
        
        public CellOcclusion OcclusionObject { private get; set; }
        
        public List<Portal> Portals { get; private set; } = new();
        
        public void SetVisibility(bool visible)
        {
            IsVisible = visible;
            foreach (var roomObject in RoomObjects)
            {
                roomObject.SetActive(visible);
            }
        }
        
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                OcclusionObject.AddCurrentRoom(FormId, this);
            }
        }
        
        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                OcclusionObject.RemoveCurrentRoom(FormId, this);
            }
        }
    }
}