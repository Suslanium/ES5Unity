using System;
using System.Collections;
using NIF.Parser;
using NIF.Parser.NiObjects;
using UnityEngine;

namespace NIF.Builder.Delegate
{
    public delegate IEnumerator InstantiateChildNiObjectDelegate(NiObject niObject, Action<GameObject> onReadyCallback);
  
    //Only needed to store different niObjectDelegates in a list
    public interface INiObjectDelegate
    {
        bool IsApplicable(NiObject niObject);

        IEnumerator Instantiate(NiFile niFile, NiObject niObject,
            InstantiateChildNiObjectDelegate instantiateChildDelegate, Action<GameObject> onReadyCallback);
    }
}