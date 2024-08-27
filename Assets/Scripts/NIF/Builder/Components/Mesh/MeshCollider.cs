﻿using System.Collections;

namespace NIF.Builder.Components.Mesh
{
    public class MeshCollider : IComponent
    {
        public Mesh Mesh;
        
        public bool Convex = false;

        public IEnumerator Apply(UnityEngine.GameObject gameObject)
        {
            var component = gameObject.AddComponent<UnityEngine.MeshCollider>();
            yield return null;
            
            if (Mesh == null)
                yield break;
            
            component.convex = Convex;
            yield return null;
            var meshCoroutine = Mesh.Create();
            while (meshCoroutine.MoveNext())
                yield return null;
            component.sharedMesh = meshCoroutine.Current;
            yield return null;
        }
    }
}