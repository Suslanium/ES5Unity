using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace Engine
{
    /// <summary>
    /// The whole purpose of this script is to apply environmental maps to objects using Unity's reflection probes (Current standard shaders have no options for cubemap reflections, and legacy reflective shaders are just not enough(For example, there is no legacy shader that allows to use diffuse, specular, normal, environmental and glow map at the same time))
    /// </summary>
    public class EnvironmentalMapManager
    {
        private readonly TextureManager _textureManager;
        private readonly Dictionary<string, GameObject> _reflectionProbes = new();
        private static readonly Vector3 DefaultTransform = new(0, -5000, 0);
        private const float ProbeYStep = 30f;

        public EnvironmentalMapManager(TextureManager textureManager)
        {
            _textureManager = textureManager;
        }

        public void ApplyEnvironmentalMapToMeshRenderer(MeshRenderer meshRenderer, string envMapPath)
        {
            if (string.IsNullOrEmpty(envMapPath))
            {
                return;
            }
            if (_reflectionProbes.TryGetValue(envMapPath, out var refProbe))
            {
                meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Simple;
                meshRenderer.probeAnchor = refProbe.transform;
                return;
            }

            var envMapTexture = _textureManager.GetEnvMap(envMapPath);
            var reflectionProbeGameObject = new GameObject(Path.GetFileNameWithoutExtension(envMapPath))
            {
                transform =
                {
                    position = new Vector3(DefaultTransform.x,
                        DefaultTransform.y + ProbeYStep * _reflectionProbes.Count, DefaultTransform.z)
                }
            };
            var reflectionProbe = reflectionProbeGameObject.AddComponent<ReflectionProbe>();
            reflectionProbe.mode = ReflectionProbeMode.Custom;
            reflectionProbe.customBakedTexture = envMapTexture;
            meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Simple;
            meshRenderer.probeAnchor = reflectionProbe.transform;
            _reflectionProbes.Add(envMapPath, reflectionProbeGameObject);
        }

        public void DestroyAllCubeMaps()
        {
            foreach (var reflectionProbe in _reflectionProbes.Values)
            {
                Object.Destroy(reflectionProbe);
            }
            _reflectionProbes.Clear();
        }
    }
}