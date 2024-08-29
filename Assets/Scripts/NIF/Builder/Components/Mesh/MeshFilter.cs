using System.Collections;
using Coroutine = Engine.Core.Coroutine;

namespace NIF.Builder.Components.Mesh
{
    public class MeshFilter : IComponent
    {
        public Mesh Mesh;

        public IEnumerator Apply(UnityEngine.GameObject gameObject)
        {
            var component = gameObject.AddComponent<UnityEngine.MeshFilter>();
            yield return null;

            if (Mesh == null)
                yield break;

            UnityEngine.Mesh mesh = null;
            yield return null;
            var meshCoroutine = Coroutine.Get(Mesh.Create(createdMesh => { mesh = createdMesh; }), nameof(Mesh.Create));
            while (meshCoroutine.MoveNext())
                yield return null;
            component.mesh = mesh;
            yield return null;
        }
    }
}