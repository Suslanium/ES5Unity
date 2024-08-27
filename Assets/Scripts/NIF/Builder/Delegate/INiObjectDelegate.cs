using NIF.Parser;
using NIF.Parser.NiObjects;
using GameObject = NIF.Builder.Components.GameObject;

namespace NIF.Builder.Delegate
{
    public delegate GameObject InstantiateChildNiObjectDelegate(NiObject niObject);
  
    //Only needed to store different niObjectDelegates in a list
    public interface INiObjectDelegate
    {
        bool IsApplicable(NiObject niObject);

        GameObject Instantiate(NiFile niFile, NiObject niObject,
            InstantiateChildNiObjectDelegate instantiateChildDelegate);
    }
}