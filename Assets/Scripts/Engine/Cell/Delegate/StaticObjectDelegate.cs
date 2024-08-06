using System.Collections;
using Engine.Cell.Delegate.Reference;
using MasterFile.MasterFileContents;
using MasterFile.MasterFileContents.Records;
using UnityEngine;

namespace Engine.Cell.Delegate
{
    public class StaticObjectDelegate : ICellReferencePreprocessDelegate, ICellReferenceInstantiationDelegate
    {
        private readonly NifManager _nifManager;
        
        public StaticObjectDelegate(NifManager nifManager)
        {
            _nifManager = nifManager;
        }

        public bool IsPreprocessApplicable(CELL cell, LoadCause loadCause, REFR reference, Record referencedRecord)
        {
            return referencedRecord is STAT or MSTT or FURN or TREE;
        }

        public IEnumerator PreprocessObject(CELL cell, GameObject cellGameObject, LoadCause loadCause, REFR reference,
            Record referencedRecord)
        {
            switch (referencedRecord)
            {
                case STAT stat:
                    _nifManager.PreloadNifFile(stat.NifModelFilename);
                    break;
                case MSTT mstt:
                    _nifManager.PreloadNifFile(mstt.NifModelFilename);
                    break;
                case FURN furn:
                    _nifManager.PreloadNifFile(furn.NifModelFilename);
                    break;
                case TREE tree:
                    _nifManager.PreloadNifFile(tree.NifModelFilename);
                    break;
            }

            yield break;
        }

        public bool IsInstantiationApplicable(CELL cell, LoadCause loadCause, REFR reference, Record referencedRecord)
        {
            return referencedRecord is STAT or MSTT or FURN or TREE;
        }

        public IEnumerator InstantiateObject(CELL cell, GameObject cellGameObject, LoadCause loadCause, REFR reference,
            Record referencedRecord)
        {
            var instantiationCoroutine = referencedRecord switch
            {
                STAT stat => InstantiateModelAtPositionAndRotation(stat.NifModelFilename, reference.Position,
                    reference.Rotation, reference.Scale, cellGameObject),
                MSTT mstt => InstantiateModelAtPositionAndRotation(mstt.NifModelFilename, reference.Position,
                    reference.Rotation, reference.Scale, cellGameObject),
                FURN furn => InstantiateModelAtPositionAndRotation(furn.NifModelFilename, reference.Position,
                    reference.Rotation, reference.Scale, cellGameObject),
                TREE tree => InstantiateModelAtPositionAndRotation(tree.NifModelFilename, reference.Position,
                    reference.Rotation, reference.Scale, cellGameObject),
                _ => null
            };

            if (instantiationCoroutine == null) yield break;
            
            while (instantiationCoroutine.MoveNext())
            {
                yield return null;
            }
        }

        private IEnumerator InstantiateModelAtPositionAndRotation(string modelPath, float[] position, float[] rotation,
            float scale, GameObject parent)
        {
            var modelObjectCoroutine = _nifManager.InstantiateNif(modelPath,
                modelObject => { CellUtils.ApplyPositionAndRotation(position, rotation, scale, parent, modelObject); });
            while (modelObjectCoroutine.MoveNext())
            {
                yield return null;
            }
        }
    }
}