using System;
using System.Collections;
using NIF.NiObjects;
using UnityEngine;

namespace NIF.Converter.Delegate.Collision
{
    public class BhkRigidBodyDelegate : NiObjectDelegate<BhkRigidBody>
    {
        protected override IEnumerator Instantiate(NiFile niFile, BhkRigidBody niObject, InstantiateChildNiObjectDelegate instantiateChildDelegate,
            Action<GameObject> onReadyCallback)
        {
            var shapeInfo = niFile.NiObjects[niObject.ShapeReference];
            return instantiateChildDelegate(shapeInfo, onReadyCallback);
        }
    }
}