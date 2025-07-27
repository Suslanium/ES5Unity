using NIF.Parser;
using NIF.Parser.NiObjects;
using GameObject = NIF.Builder.Components.GameObject;
using Logger = Engine.Core.Logger;

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
            Logger.LogError($"Expected {typeof(T).Name} but got {niObject.GetType().Name}");
            return null;
        }

        protected abstract GameObject Instantiate(NiFile niFile, T niObject,
            InstantiateChildNiObjectDelegate instantiateChildDelegate);
    }
}