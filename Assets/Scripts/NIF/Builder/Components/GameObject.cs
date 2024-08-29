using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Logger = Engine.Core.Logger;
using Coroutine = Engine.Core.Coroutine;

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
                    Logger.LogError("Parent can only be set once.");
                }
            }
        }

        public Vector3 Position = Vector3.zero;

        public Quaternion Rotation = Quaternion.identity;

        public Vector3 Scale = Vector3.one;

        private readonly List<GameObject> _children = new();

        private void AddChild(GameObject child)
        {
            _children.Add(child);
        }

        private readonly List<IComponent> _components = new();

        public void AddComponent(IComponent component)
        {
            _components.Add(component);
        }

        public GameObject(string name)
        {
            _name = name;
        }

        public IEnumerator Create(UnityEngine.GameObject parent, Action<UnityEngine.GameObject> onReadyCallback,
            bool isStatic = false)
        {
            if (_children.Count == 0 && _components.Count == 0)
            {
                // No need to create an empty game object
                onReadyCallback(null);
                yield break;
            }

            var gameObject = new UnityEngine.GameObject(_name)
            {
                isStatic = isStatic
            };
            yield return null;
            gameObject.transform.position = Position;
            yield return null;
            gameObject.transform.rotation = Rotation;
            yield return null;
            gameObject.transform.localScale = Scale;
            yield return null;
            gameObject.transform.SetParent(parent.transform, false);
            yield return null;

            foreach (var applyCoroutine in _components.Select(component =>
                         Coroutine.Get(component.Apply(gameObject), nameof(component.Apply))))
            {
                while (applyCoroutine.MoveNext())
                    yield return null;
            }

            foreach (var childCoroutine in _children.Select(child =>
                         Coroutine.Get(child.Create(gameObject, _ => { }, isStatic), nameof(child.Create))
                     ))

            {
                while (childCoroutine.MoveNext())
                    yield return null;
            }

            onReadyCallback(gameObject);
        }
    }
}