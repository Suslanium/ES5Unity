using Engine.Core;
using NIF.Builder;
using UnityEngine;

namespace Engine
{
    public struct WorldSpacePosition
    {
        /// <summary>
        /// Cell block coordinates.
        /// </summary>
        public readonly Vector2Int Block;

        /// <summary>
        /// Cell sub-block coordinates.
        /// </summary>
        public readonly Vector2Int SubBlock;

        /// <summary>
        /// Cell grid coordinates.
        /// </summary>
        public readonly Vector2Int CellGridPosition;

        /// <summary>
        /// Position in game units.
        /// </summary>
        public readonly Vector3 Position;

        public WorldSpacePosition(Vector3 position)
        {
            Position = position;
            CellGridPosition = new Vector2Int(Mathf.FloorToInt(position.x / Convert.ExteriorCellSideLengthInMWUnits),
                Mathf.FloorToInt(position.y / Convert.ExteriorCellSideLengthInMWUnits));
            SubBlock = new Vector2Int(Mathf.FloorToInt((float)CellGridPosition.x / Convert.ExteriorSubBlockSideLengthInCells),
                Mathf.FloorToInt((float)CellGridPosition.y / Convert.ExteriorSubBlockSideLengthInCells));
            Block = new Vector2Int(Mathf.FloorToInt((float)SubBlock.x / Convert.ExteriorBlockSideLengthInSubBlocks),
                Mathf.FloorToInt((float)SubBlock.y / Convert.ExteriorBlockSideLengthInSubBlocks));
        }
    }

    public class PlayerManager
    {
        private readonly GameObject _player;

        public bool PlayerActive
        {
            get => _player.activeSelf;
            set => _player.SetActive(value);
        }

        public WorldSpacePosition WorldPlayerPosition =>
            new WorldSpacePosition(NifUtils.UnityPointToNifPoint(_player.transform.position));

        public Vector3 PlayerPosition
        {
            get => _player.transform.position;
            set => _player.transform.position = value;
        }
        
        public Quaternion PlayerRotation
        {
            get => _player.transform.rotation;
            set => _player.transform.rotation = value;
        }
        
        public Collider PlayerCollider { get; }

        public PlayerManager(GameObject player)
        {
            _player = player;
            PlayerCollider = player.GetComponentInChildren<Collider>();
        }
    }
}