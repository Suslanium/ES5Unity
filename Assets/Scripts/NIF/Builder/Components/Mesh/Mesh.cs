using System;
using System.Collections;
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

        public IEnumerator Create(Action<UnityEngine.Mesh> onReadyCallback)
        {
            var mesh = new UnityEngine.Mesh();

            if (Vertices != null)
                mesh.vertices = Vertices;
            yield return null;
            if (Triangles != null)
                mesh.triangles = Triangles;
            yield return null;
            if (Normals != null)
                mesh.normals = Normals;
            yield return null;
            if (Tangents != null)
                mesh.tangents = Tangents;
            yield return null;
            if (UVs != null)
                mesh.uv = UVs;
            yield return null;
            if (Colors != null)
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

            onReadyCallback(mesh);
        }
    }
}