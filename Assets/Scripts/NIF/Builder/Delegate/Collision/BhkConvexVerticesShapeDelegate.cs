using System.Linq;
using NIF.Parser;
using NIF.Parser.NiObjects;
using GameObject = NIF.Builder.Components.GameObject;
using Mesh = NIF.Builder.Components.Mesh.Mesh;
using MeshCollider = NIF.Builder.Components.Mesh.MeshCollider;

namespace NIF.Builder.Delegate.Collision
{
    public class BhkConvexVerticesShapeDelegate : NiObjectDelegate<BhkConvexVerticesShape>
    {
        protected override GameObject Instantiate(NiFile niFile, BhkConvexVerticesShape niObject,
            InstantiateChildNiObjectDelegate instantiateChildDelegate)
        {
            var gameObject = new GameObject("bhkConvexVerticesShape");
            var vertices = niObject.Vertices
                .Select(vertex => NifUtils.NifVectorToUnityVector(vertex.ToUnityVector())).ToArray();
            var mesh = new Mesh
            {
                Vertices = vertices
            };
            var collider = new MeshCollider
            {
                Convex = true,
                Mesh = mesh
            };
            gameObject.AddComponent(collider);
            return gameObject;
        }
    }
}