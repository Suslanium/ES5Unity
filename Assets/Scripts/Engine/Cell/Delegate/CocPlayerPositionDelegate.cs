using System.Collections;
using Engine.Cell.Delegate.Interfaces;
using MasterFile.MasterFileContents;
using MasterFile.MasterFileContents.Records;
using NIF.Builder;
using UnityEngine;

namespace Engine.Cell.Delegate
{
    public class CocPlayerPositionDelegate : ICellReferencePreprocessDelegate
    {
        private readonly GameObject _player;
        private const uint PlayerCocPositionRecordFormID = 0x32;
        
        public CocPlayerPositionDelegate(GameObject player)
        {
            _player = player;
        }
        
        public bool IsPreprocessApplicable(CELL cell, LoadCause loadCause, REFR reference, Record referencedRecord)
        {
            return loadCause == LoadCause.Coc && referencedRecord is STAT { FormID: PlayerCocPositionRecordFormID };
        }

        public IEnumerator PreprocessObject(CELL cell, GameObject cellGameObject, LoadCause loadCause, REFR reference,
            Record referencedRecord)
        {
            _player.transform.position = NifUtils.NifPointToUnityPoint(new Vector3(reference.Position[0],
                reference.Position[1], reference.Position[2]));
            _player.transform.rotation = NifUtils.NifEulerAnglesToUnityQuaternion(
                new Vector3(reference.Rotation[0], reference.Rotation[1], reference.Rotation[2]));
            
            yield break;
        }
    }
}