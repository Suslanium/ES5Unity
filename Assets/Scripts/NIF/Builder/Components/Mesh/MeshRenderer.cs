using System.Collections;
using Engine.Textures;
using UnityEngine;
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

            Material material = null;
            yield return null;

            var materialCoroutine = Coroutine.Get(_materialManager.GetMaterialFromProperties(MaterialProperties.Value,
                    createdMaterial => { material = createdMaterial; }),
                nameof(_materialManager.GetMaterialFromProperties));
            while (materialCoroutine.MoveNext())
                yield return null;

            renderer.sharedMaterial = material;
            yield return null;
        }
    }
}