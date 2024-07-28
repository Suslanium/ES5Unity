using System;
using System.Collections;
using System.Linq;
using NIF.NiObjects;
using UnityEngine;

namespace NIF.Converter.Delegate.Collision
{
    public class BhkListShapeDelegate : NiObjectDelegate<BhkListShape>
    {
        protected override IEnumerator Instantiate(NiFile niFile, BhkListShape niObject,
            InstantiateChildNiObjectDelegate instantiateChildDelegate,
            Action<GameObject> onReadyCallback)
        {
            var shapeRefs = niObject.SubShapeReferences.Select(subShapeRef => niFile.NiObjects[subShapeRef]);
            var rootGameObject = new GameObject("bhkListShape");
            yield return null;
            foreach (var subShape in shapeRefs)
            {
                var shapeObjectCoroutine = instantiateChildDelegate(subShape,
                    shapeObject => { shapeObject.transform.SetParent(rootGameObject.transform, false); });
                if (shapeObjectCoroutine == null) continue;

                while (shapeObjectCoroutine.MoveNext())
                {
                    yield return null;
                }
            }

            onReadyCallback(rootGameObject);
        }
    }
}