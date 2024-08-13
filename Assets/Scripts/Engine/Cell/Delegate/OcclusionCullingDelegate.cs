using System.Collections;
using System.Collections.Generic;
using Engine.Cell.Delegate.Interfaces;
using Engine.Cell.Delegate.Reference;
using Engine.Occlusion;
using MasterFile.MasterFileContents;
using MasterFile.MasterFileContents.Records;
using NIF.Builder;
using UnityEngine;

namespace Engine.Cell.Delegate
{
    public class OcclusionCullingDelegate : ICellReferencePreprocessDelegate, ICellPostProcessDelegate
    {
        private static readonly int RoomLayer = LayerMask.NameToLayer("Room");
        private static readonly int PortalLayer = LayerMask.NameToLayer("Portal");
        private readonly List<(GameObject, uint, uint)> _tempPortals = new();
        private readonly Dictionary<uint, GameObject> _tempRooms = new();
        private readonly List<(uint, uint)> _tempLinkedRooms = new();
        private readonly PlayerManager _playerManager;
        private readonly GameEngine _gameEngine;
        private const uint PortalFormID = 0x20;
        private const uint RoomFormID = 0x1F;

        public OcclusionCullingDelegate(PlayerManager playerManager, GameEngine gameEngine)
        {
            _playerManager = playerManager;
            _gameEngine = gameEngine;
        }

        public bool IsPreprocessApplicable(CELL cell, REFR reference, Record referencedRecord)
        {
            return referencedRecord is STAT { FormID: PortalFormID or RoomFormID };
        }

        public IEnumerator PreprocessObject(CELL cell, GameObject cellGameObject, REFR reference,
            Record referencedRecord)
        {
            if (referencedRecord is not STAT staticRecord)
                yield break;
            if (staticRecord.FormID == 0x20)
            {
                //Portal marker
                var gameObject = new GameObject("Portal marker")
                {
                    layer = PortalLayer
                };
                var collider = gameObject.AddComponent<BoxCollider>();
                collider.isTrigger = true;
                collider.size = NifUtils.NifPointToUnityPoint(reference.Primitive.Bounds) * 2;
                CellUtils.ApplyPositionAndRotation(reference.Position, reference.Rotation, reference.Scale,
                    cellGameObject,
                    gameObject);
                if (reference.PortalDestinations != null)
                {
                    _tempPortals.Add((gameObject, reference.PortalDestinations.OriginReference,
                        reference.PortalDestinations.DestinationReference));
                }
                else
                {
                    gameObject.SetActive(false);
                }

                yield break;
            }

            if (staticRecord.FormID == 0x1F)
            {
                //Room marker
                var gameObject = new GameObject("Room marker")
                {
                    layer = RoomLayer
                };
                var collider = gameObject.AddComponent<BoxCollider>();
                collider.isTrigger = true;
                collider.size = NifUtils.NifPointToUnityPoint(reference.Primitive.Bounds) * 2;
                CellUtils.ApplyPositionAndRotation(reference.Position, reference.Rotation, reference.Scale,
                    cellGameObject,
                    gameObject);
                _tempRooms.Add(reference.FormID, gameObject);
                yield return null;

                if (reference.LinkedRoomFormIDs.Count > 0)
                {
                    foreach (var linkedRoomFormID in reference.LinkedRoomFormIDs)
                    {
                        _tempLinkedRooms.Add((reference.FormID, linkedRoomFormID));
                    }
                }
            }
        }

        public IEnumerator PostProcessCell(CELL cell, GameObject cellGameObject)
        {
            if (_tempPortals.Count > 0 || _tempRooms.Count > 0)
            {
                var cellOcclusion = cellGameObject.AddComponent<CellOcclusion>();
                cellOcclusion.Init(_tempPortals, _tempRooms, _tempLinkedRooms, cellGameObject,
                    _playerManager.PlayerCollider, _gameEngine.MainCamera);
            }

            _tempLinkedRooms.Clear();
            _tempPortals.Clear();
            _tempRooms.Clear();

            yield break;
        }
    }
}