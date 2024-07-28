using System;
using System.Collections;
using NIF.NiObjects;
using UnityEngine;

namespace NIF.Converter.Delegate.Collision
{
    public class BvTreeShapeDelegate : NiObjectDelegate<BhkBvTreeShape>
    {
        protected override IEnumerator Instantiate(NiFile niFile, BhkBvTreeShape niObject,
            InstantiateChildNiObjectDelegate instantiateChildDelegate, Action<GameObject> onReadyCallback)
        {
            var shapeInfo = niFile.NiObjects[niObject.ShapeReference];
            return instantiateChildDelegate(shapeInfo, onReadyCallback);
        }
    }
}