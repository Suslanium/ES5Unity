using NIF.Parser;
using NIF.Parser.NiObjects;
using GameObject = NIF.Builder.Components.GameObject;

namespace NIF.Builder.Delegate.Collision
{
    public class BhkRigidBodyDelegate : NiObjectDelegate<BhkRigidBody>
    {
        protected override GameObject Instantiate(NiFile niFile, BhkRigidBody niObject,
            InstantiateChildNiObjectDelegate instantiateChildDelegate)
        {
            var shapeInfo = niFile.NiObjects[niObject.ShapeReference];
            return instantiateChildDelegate(shapeInfo);
        }
    }
}