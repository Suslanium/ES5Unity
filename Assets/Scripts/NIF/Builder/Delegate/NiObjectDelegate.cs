using System;
using System.Collections;
using NIF.Parser;
using NIF.Parser.NiObjects;
using UnityEngine;

namespace NIF.Builder.Delegate
{
    public abstract class NiObjectDelegate<T> : INiObjectDelegate where T : NiObject
    {
        public virtual bool IsApplicable(NiObject niObject)
        {
            return niObject is T;
        }

        public IEnumerator Instantiate(NiFile niFile, NiObject niObject, InstantiateChildNiObjectDelegate instantiateChildDelegate,
            Action<GameObject> onReadyCallback)
        {
            if (niObject is T niTypeObj)
                return Instantiate(niFile, niTypeObj, instantiateChildDelegate, onReadyCallback);
            Debug.LogError($"Expected {typeof(T).Name} but got {niObject.GetType().Name}");
            return null;
        }

        protected abstract IEnumerator Instantiate(NiFile niFile, T niObject,
            InstantiateChildNiObjectDelegate instantiateChildDelegate, Action<GameObject> onReadyCallback);
    }
}