using NIF.Parser;
using NIF.Parser.NiObjects;
using GameObject = NIF.Builder.Components.GameObject;

namespace NIF.Builder.Delegate.Collision
{
    public class BvTreeShapeDelegate : NiObjectDelegate<BhkBvTreeShape>
    {
        protected override GameObject Instantiate(NiFile niFile, BhkBvTreeShape niObject,
            InstantiateChildNiObjectDelegate instantiateChildDelegate)
        {
            var shapeInfo = niFile.NiObjects[niObject.ShapeReference];
            return instantiateChildDelegate(shapeInfo);
        }
    }
}