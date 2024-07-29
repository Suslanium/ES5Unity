using System;
using System.Collections;
using NIF.Parser;
using NIF.Parser.NiObjects;
using UnityEngine;

namespace NIF.Builder.Delegate.Collision
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