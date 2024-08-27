using NIF.Parser;
using NIF.Parser.NiObjects;
using UnityEngine;
using GameObject = NIF.Builder.Components.GameObject;

namespace NIF.Builder.Delegate.Collision
{
    public class BhkCollisionObjectDelegate : NiObjectDelegate<BhkCollisionObject>
    {
        protected override GameObject Instantiate(NiFile niFile, BhkCollisionObject niObject,
            InstantiateChildNiObjectDelegate instantiateChildDelegate)
        {
            var body = niFile.NiObjects[niObject.BodyReference];
            switch (body)
            {
                case BhkRigidBodyT bhkRigidBodyT:
                    var gameObject = instantiateChildDelegate(bhkRigidBodyT);

                    if (gameObject == null)
                    {
                        return null;
                    }

                    gameObject.Position =
                        NifUtils.NifVectorToUnityVector(bhkRigidBodyT.Translation.ToUnityVector());
                    gameObject.Rotation =
                        NifUtils.HavokQuaternionToUnityQuaternion(bhkRigidBodyT.Rotation.ToUnityQuaternion());
                    return gameObject;
                case BhkRigidBody bhkRigidBody:
                    return instantiateChildDelegate(bhkRigidBody);
                default:
                    Debug.LogWarning($"Unsupported collision object body type: {body.GetType().Name}");
                    return null;
            }
        }
    }
}