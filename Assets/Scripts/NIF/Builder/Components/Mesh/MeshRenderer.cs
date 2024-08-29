using System.Collections;
using Engine.Textures;
using Coroutine = Engine.Core.Coroutine;

namespace NIF.Builder.Components.Mesh
{
    public class MeshRenderer : IComponent
    {
        public MaterialProperties? MaterialProperties;
        private readonly MaterialManager _materialManager;

        public MeshRenderer(MaterialManager materialManager)
        {
            _materialManager = materialManager;
        }

        public IEnumerator Apply(UnityEngine.GameObject gameObject)
        {
            var renderer = gameObject.AddComponent<UnityEngine.MeshRenderer>();
            yield return null;

            if (MaterialProperties == null)
                yield break;

            var materialCoroutine = Coroutine.Get(_materialManager.GetMaterialFromProperties(MaterialProperties.Value),
                nameof(_materialManager.GetMaterialFromProperties));
            while (materialCoroutine.MoveNext())
                yield return null;

            var material = materialCoroutine.Current;
            yield return null;

            renderer.sharedMaterial = material;
            yield return null;
        }
    }
}