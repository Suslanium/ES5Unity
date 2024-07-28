using System;
using System.Collections;
using NIF.NiObjects;
using UnityEngine;

namespace NIF.Converter.Delegate.Collision
{
    public class BhkCompressedMeshShapeDelegate : NiObjectDelegate<BhkCompressedMeshShape>
    {
        protected override IEnumerator Instantiate(NiFile niFile, BhkCompressedMeshShape niObject,
            InstantiateChildNiObjectDelegate instantiateChildDelegate, Action<GameObject> onReadyCallback)
        {
            var shapeData = niFile.NiObjects[niObject.DataRef];
            GameObject shapeObject = null;
            var shapeObjectCoroutine = instantiateChildDelegate(shapeData,
                o => { shapeObject = o; });
            if (shapeObjectCoroutine == null)
            {
                onReadyCallback(null);
                yield break;
            }

            while (shapeObjectCoroutine.MoveNext())
            {
                yield return null;
            }

            shapeObject.transform.localScale =
                NifUtils.NifVectorToUnityVector(niObject.Scale.ToUnityVector());
            onReadyCallback(shapeObject);
        }
    }
}