using NIF.Parser;
using NIF.Parser.NiObjects;
using GameObject = NIF.Builder.Components.GameObject;

namespace NIF.Builder.Delegate
{
    public class NiNodeDelegate : NiObjectDelegate<NiNode>
    {
        protected override GameObject Instantiate(NiFile niFile, NiNode niObject,
            InstantiateChildNiObjectDelegate instantiateChildDelegate)
        {
            var gameObject = new GameObject(niObject.Name);

            foreach (var childRef in niObject.ChildrenReferences)
            {
                if (childRef < 0) continue;
                var child = instantiateChildDelegate(niFile.NiObjects[childRef]);

                if (child != null)
                {
                    child.Parent = gameObject;
                }
            }

            NifUtils.ApplyNiAvObjectTransform(niObject, gameObject);

            return gameObject;
        }
    }
}