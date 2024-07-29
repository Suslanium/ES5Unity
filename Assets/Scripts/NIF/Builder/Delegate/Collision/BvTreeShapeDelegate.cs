using System;
using System.Collections;
using NIF.Parser;
using NIF.Parser.NiObjects;
using UnityEngine;

namespace NIF.Builder.Delegate.Collision
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