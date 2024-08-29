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
            
            var meshCoroutine = Coroutine.Get(Mesh.Create(), nameof(Mesh.Create));
            while (meshCoroutine.MoveNext())
                yield return null;
            component.mesh = meshCoroutine.Current;
            yield return null;
        }
    }
}