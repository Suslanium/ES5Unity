using System.Collections;
using Engine.Cell.Delegate.Reference;
using MasterFile.MasterFileContents;
using MasterFile.MasterFileContents.Records;
using UnityEngine;
using Coroutine = Engine.Core.Coroutine;

namespace Engine.Cell.Delegate
{
    public class StaticObjectDelegate : ICellReferencePreprocessDelegate, ICellReferenceInstantiationDelegate
    {
        private readonly NifManager _nifManager;

        public StaticObjectDelegate(NifManager nifManager)
        {
            _nifManager = nifManager;
        }

        public bool IsPreprocessApplicable(CELL cell, REFR reference, Record referencedRecord)
        {
            return referencedRecord is STAT or MSTT or FURN or TREE;
        }

        public IEnumerator PreprocessObject(CELL cell, GameObject cellGameObject, REFR reference,
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

        public bool IsInstantiationApplicable(CELL cell, REFR reference, Record referencedRecord)
        {
            return referencedRecord is STAT or MSTT or FURN or TREE;
        }

        public IEnumerator InstantiateObject(CELL cell, GameObject cellGameObject, REFR reference,
            Record referencedRecord)
        {
            var instantiationCoroutine = referencedRecord switch
            {
                STAT stat => Coroutine.Get(InstantiateModelAtPositionAndRotation(stat.NifModelFilename,
                        reference.Position,
                        reference.Rotation, reference.Scale, cellGameObject),
                    nameof(InstantiateModelAtPositionAndRotation)),
                MSTT mstt => Coroutine.Get(InstantiateModelAtPositionAndRotation(mstt.NifModelFilename,
                        reference.Position,
                        reference.Rotation, reference.Scale, cellGameObject),
                    nameof(InstantiateModelAtPositionAndRotation)),
                FURN furn => Coroutine.Get(InstantiateModelAtPositionAndRotation(furn.NifModelFilename,
                        reference.Position,
                        reference.Rotation, reference.Scale, cellGameObject),
                    nameof(InstantiateModelAtPositionAndRotation)),
                TREE tree => Coroutine.Get(InstantiateModelAtPositionAndRotation(tree.NifModelFilename,
                        reference.Position,
                        reference.Rotation, reference.Scale, cellGameObject),
                    nameof(InstantiateModelAtPositionAndRotation)),
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
            var modelObjectCoroutine =
                Coroutine.Get(_nifManager.InstantiateNif(modelPath), nameof(_nifManager.InstantiateNif));
            while (modelObjectCoroutine.MoveNext())
            {
                yield return null;
            }

            var modelObject = modelObjectCoroutine.Current;
            yield return null;
            CellUtils.ApplyPositionAndRotation(position, rotation, scale, parent, modelObject);
            yield return null;
        }
    }
}