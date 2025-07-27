using NIF.Parser;
using NIF.Parser.NiObjects;
using GameObject = NIF.Builder.Components.GameObject;

namespace NIF.Builder.Delegate.Collision
{
    public class BhkCompressedMeshShapeDelegate : NiObjectDelegate<BhkCompressedMeshShape>
    {
        protected override GameObject Instantiate(NiFile niFile, BhkCompressedMeshShape niObject,
            InstantiateChildNiObjectDelegate instantiateChildDelegate)
        {
            var shapeData = niFile.NiObjects[niObject.DataRef];
            var shapeObject = instantiateChildDelegate(shapeData);

            if (shapeObject == null)
            {
                return null;
            }

            shapeObject.Scale =
                NifUtils.NifVectorToUnityVector(niObject.Scale.ToUnityVector());
            return shapeObject;
        }
    }
}