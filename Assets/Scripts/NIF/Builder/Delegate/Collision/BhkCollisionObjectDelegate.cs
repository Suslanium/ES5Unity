using System;
using System.Collections;
using NIF.Parser;
using NIF.Parser.NiObjects;
using UnityEngine;

namespace NIF.Builder.Delegate.Collision
{
    public class BhkCollisionObjectDelegate : NiObjectDelegate<BhkCollisionObject>
    {
        protected override IEnumerator Instantiate(NiFile niFile, BhkCollisionObject niObject,
            InstantiateChildNiObjectDelegate instantiateChildDelegate, Action<GameObject> onReadyCallback)
        {
            var body = niFile.NiObjects[niObject.BodyReference];
            switch (body)
            {
                case BhkRigidBodyT bhkRigidBodyT:
                    GameObject gameObject = null;
                    var gameObjectCoroutine = instantiateChildDelegate(bhkRigidBodyT,
                        o => { gameObject = o; });
                    if (gameObjectCoroutine == null)
                    {
                        onReadyCallback(null);
                        yield break;
                    }

                    while (gameObjectCoroutine.MoveNext())
                    {
                        yield return null;
                    }

                    if (gameObject == null)
                    {
                        onReadyCallback(null);
                        yield break;
                    }

                    gameObject.transform.position =
                        NifUtils.NifVectorToUnityVector(bhkRigidBodyT.Translation.ToUnityVector());
                    gameObject.transform.rotation =
                        NifUtils.HavokQuaternionToUnityQuaternion(bhkRigidBodyT.Rotation.ToUnityQuaternion());
                    onReadyCallback(gameObject);
                    break;
                case BhkRigidBody bhkRigidBody:
                    var rigidBodyCoroutine = instantiateChildDelegate(bhkRigidBody, onReadyCallback);
                    if (rigidBodyCoroutine == null)
                    {
                        onReadyCallback(null);
                        yield break;
                    }

                    while (rigidBodyCoroutine.MoveNext())
                    {
                        yield return null;
                    }

                    break;
                default:
                    Debug.LogWarning($"Unsupported collision object body type: {body.GetType().Name}");
                    onReadyCallback(null);
                    break;
            }
        }
    }
}