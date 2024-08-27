using System.Linq;
using NIF.Parser;
using NIF.Parser.NiObjects;
using GameObject = NIF.Builder.Components.GameObject;

namespace NIF.Builder.Delegate.Collision
{
    public class BhkListShapeDelegate : NiObjectDelegate<BhkListShape>
    {
        protected override GameObject Instantiate(NiFile niFile, BhkListShape niObject,
            InstantiateChildNiObjectDelegate instantiateChildDelegate)
        {
            var shapeRefs = niObject.SubShapeReferences.Select(subShapeRef => niFile.NiObjects[subShapeRef]);
            var rootGameObject = new GameObject("bhkListShape");
            foreach (var subShape in shapeRefs)
            {
                var shapeObject = instantiateChildDelegate(subShape);
                if (shapeObject == null)
                    continue;

                shapeObject.Parent = rootGameObject;
            }

            return rootGameObject;
        }
    }
}