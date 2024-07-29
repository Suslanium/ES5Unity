using System;
using System.Collections;
using NIF.Parser;
using NIF.Parser.NiObjects;
using UnityEngine;

namespace NIF.Builder.Delegate
{
    public class NiNodeDelegate : NiObjectDelegate<NiNode>
    {
        protected override IEnumerator Instantiate(NiFile niFile, NiNode niObject,
            InstantiateChildNiObjectDelegate instantiateChildDelegate,
            Action<GameObject> onReadyCallback)
        {
            var gameObject = new GameObject(niObject.Name);

            foreach (var childRef in niObject.ChildrenReferences)
            {
                if (childRef < 0) continue;
                var childCoroutine = instantiateChildDelegate(niFile.NiObjects[childRef], child =>
                {
                    if (child != null)
                    {
                        child.transform.SetParent(gameObject.transform, false);
                    }
                });

                if (childCoroutine == null) continue;
                while (childCoroutine.MoveNext())
                {
                    yield return null;
                }
            }

            NifUtils.ApplyNiAvObjectTransform(niObject, gameObject);

            onReadyCallback(gameObject);
        }
    }
}