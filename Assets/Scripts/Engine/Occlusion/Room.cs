﻿using System.Collections.Generic;
using UnityEngine;

namespace Engine.Occlusion
{
    public class Room: MonoBehaviour
    {
        public uint FormId { get; set; }
        
        public CellOcclusion OcclusionObject { private get; set; }
        
        public List<Portal> Portals { get; private set; } = new();

        //public Dictionary<uint, Room> NonPortalConnections { get; private set; } = new();
        
        //public BoxCollider RoomTrigger { get; set; }

        //private void Start()
        //{
            //RoomTrigger = GetComponent<BoxCollider>();
        //}

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
                OcclusionObject.RemoveCurrentRoom(FormId);
            }
        }
    }
}