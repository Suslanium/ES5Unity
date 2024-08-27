using System.Collections;

namespace NIF.Builder.Components.Mesh
{
    public class MeshFilter : IComponent
    {
        public Mesh Mesh;
        
        public IEnumerator Apply(UnityEngine.GameObject gameObject)
        {
            if (Mesh == null)
                yield break;
            
            var component = gameObject.AddComponent<UnityEngine.MeshFilter>();
            yield return null;
            var meshCoroutine = Mesh.Create();
            while (meshCoroutine.MoveNext())
                yield return null;
            component.mesh = meshCoroutine.Current;
            yield return null;
        }
    }
}