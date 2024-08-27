using NIF.Parser;
using NIF.Parser.NiObjects;
using UnityEngine;
using GameObject = NIF.Builder.Components.GameObject;

namespace NIF.Builder.Delegate
{
    public abstract class NiObjectDelegate<T> : INiObjectDelegate where T : NiObject
    {
        public virtual bool IsApplicable(NiObject niObject)
        {
            return niObject is T;
        }

        public GameObject Instantiate(NiFile niFile, NiObject niObject,
            InstantiateChildNiObjectDelegate instantiateChildDelegate)
        {
            if (niObject is T niTypeObj)
                return Instantiate(niFile, niTypeObj, instantiateChildDelegate);
            Debug.LogError($"Expected {typeof(T).Name} but got {niObject.GetType().Name}");
            return null;
        }

        protected abstract GameObject Instantiate(NiFile niFile, T niObject,
            InstantiateChildNiObjectDelegate instantiateChildDelegate);
    }
}