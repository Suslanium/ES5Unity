using System.Collections;
using Engine.Cell.Delegate.Interfaces;
using Engine.Cell.Delegate.Reference;
using MasterFile.MasterFileContents;
using MasterFile.MasterFileContents.Records;
using NIF.Builder;
using UnityEngine;

namespace Engine.Cell.Delegate
{
    public class CocPlayerPositionDelegate : ICellReferencePreprocessDelegate, ICellPostProcessDelegate
    {
        private readonly GameObject _player;
        private const uint CocMarkerFormID = 0x32;
        private const string Marker = "Marker";
        private Vector3? _tempPosition;
        private Quaternion? _tempRotation;

        public CocPlayerPositionDelegate(GameObject player)
        {
            _player = player;
        }

        public bool IsPreprocessApplicable(CELL cell, LoadCause loadCause, REFR reference, Record referencedRecord)
        {
            return loadCause == LoadCause.Coc && (referencedRecord is STAT { FormID: CocMarkerFormID } ||
                                                  (_tempPosition == null && _tempRotation == null &&
                                                   reference.EditorID != null && reference.EditorID.Contains(Marker)));
        }

        public IEnumerator PreprocessObject(CELL cell, GameObject cellGameObject, LoadCause loadCause, REFR reference,
            Record referencedRecord)
        {
            _tempPosition = NifUtils.NifPointToUnityPoint(new Vector3(reference.Position[0],
                reference.Position[1], reference.Position[2]));
            _tempRotation = NifUtils.NifEulerAnglesToUnityQuaternion(
                new Vector3(reference.Rotation[0], reference.Rotation[1], reference.Rotation[2]));

            yield break;
        }

        public IEnumerator PostProcessCell(CELL cell, GameObject cellGameObject)
        {
            if (_tempPosition == null || _tempRotation == null) yield break;
            _player.transform.position = _tempPosition.Value;
            _player.transform.rotation = _tempRotation.Value;
        }
    }
}