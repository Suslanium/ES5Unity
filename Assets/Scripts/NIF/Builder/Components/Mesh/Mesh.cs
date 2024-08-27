using System.Collections.Generic;
using UnityEngine;

namespace NIF.Builder.Components.Mesh
{
    public class Mesh
    {
        public Vector3[] Vertices = null;
        public int[] Triangles = null;
        public Vector3[] Normals = null;
        public Vector4[] Tangents = null;
        public Vector2[] UVs = null;
        public Color[] Colors = null;
        public bool? HasNormals = null;
        public bool ShouldRecalculateBounds = false;

        public IEnumerator<UnityEngine.Mesh> Create()
        {
            var mesh = new UnityEngine.Mesh();
            
            mesh.vertices = Vertices;
            yield return null;
            mesh.triangles = Triangles;
            yield return null;
            mesh.normals = Normals;
            yield return null;
            mesh.tangents = Tangents;
            yield return null;
            mesh.uv = UVs;
            yield return null;
            mesh.colors = Colors;
            yield return null;

            if (HasNormals.HasValue && !HasNormals.Value)
            {
                mesh.RecalculateNormals();
                yield return null;
            }

            if (ShouldRecalculateBounds)
            {
                mesh.RecalculateBounds();
                yield return null;
            }

            yield return mesh;
        }
    }
}