using System;
using System.Collections;
using System.Linq;
using NIF.Parser;
using NIF.Parser.NiObjects;
using UnityEngine;

namespace NIF.Builder.Delegate.Collision
{
    public class BhkConvexVerticesShapeDelegate : NiObjectDelegate<BhkConvexVerticesShape>
    {
        protected override IEnumerator Instantiate(NiFile niFile, BhkConvexVerticesShape niObject,
            InstantiateChildNiObjectDelegate instantiateChildDelegate, Action<GameObject> onReadyCallback)
        {
            var gameObject = new GameObject("bhkConvexVerticesShape");
            var vertices = niObject.Vertices
                .Select(vertex => NifUtils.NifVectorToUnityVector(vertex.ToUnityVector())).ToArray();
            yield return null;
            var mesh = new Mesh
            {
                vertices = vertices
            };
            yield return null;
            var collider = gameObject.AddComponent<MeshCollider>();
            collider.convex = true;
            collider.sharedMesh = mesh;
            yield return null;
            onReadyCallback(gameObject);
        }
    }
}