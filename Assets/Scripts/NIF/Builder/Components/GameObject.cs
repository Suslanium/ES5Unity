using System.Collections.Generic;
using UnityEngine;

namespace NIF.Builder.Components
{
    /// <summary>
    /// A data structure that represents Unity's GameObject.
    /// Used to reduce the load from the main thread.
    /// (Unity's API, such as GameObject, can only be called from the main thread,
    /// this structure allows to prepare the game object in a background thread
    /// and then create it doing as few calls as possible in the main thread)
    /// </summary>
    public class GameObject
    {
        private string _name;
        
        private GameObject _parent;

        public GameObject Parent
        {
            get => _parent;
            set
            {
                if (_parent == null)
                {
                    _parent = value;
                    _parent.AddChild(this);
                }
                else
                {
                    Debug.LogError("Parent can only be set once.");
                }
            }
        }

        public Vector3 Position;

        public Quaternion Rotation;

        public Vector3 Scale;

        private readonly List<GameObject> Children = new();
        
        private void AddChild(GameObject child)
        {
            Children.Add(child);
        }

        private readonly List<IComponent> Components = new();
        
        public void AddComponent(IComponent component)
        {
            Components.Add(component);
        }
        
        public GameObject(string name)
        {
            _name = name;
        }

        public IEnumerator<UnityEngine.GameObject> Create(UnityEngine.GameObject parent)
        {
            var gameObject = new UnityEngine.GameObject(_name);
            yield return null;
            gameObject.transform.position = Position;
            yield return null;
            gameObject.transform.rotation = Rotation;
            yield return null;
            gameObject.transform.localScale = Scale;
            yield return null;
            gameObject.transform.SetParent(parent.transform, false);
            yield return null;
            
            foreach (var component in Components)
            {
                var applyCoroutine = component.Apply(gameObject);
                while (applyCoroutine.MoveNext())
                    yield return null;
            }

            foreach (var child in Children)
            {
                var childCoroutine = child.Create(gameObject);
                while (childCoroutine.MoveNext())
                    yield return null;
            }

            yield return gameObject;
        }
    }
}