using System.Collections;

namespace NIF.Builder.Components
{
    public interface IComponent
    {
        public IEnumerator Apply(UnityEngine.GameObject gameObject);
    }
}