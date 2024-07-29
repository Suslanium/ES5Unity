using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Engine;
using NIF.Builder.Delegate;
using NIF.Builder.Delegate.Collision;
using NIF.Parser;
using NIF.Parser.NiObjects;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

namespace NIF.Builder
{
    /// <summary>
    /// Based on https://github.com/ColeDeanShepherd/TESUnity/blob/f4d5e19f68da380da9da745356c7904f3428b9d6/Assets/Scripts/TES/NIF/NIFObjectBuilder.cs
    /// </summary>
    public class NifObjectBuilder
    {
        private readonly NiFile _file;
        private readonly List<INiObjectDelegate> _modelDelegates;
        private readonly List<INiObjectDelegate> _collisionDelegates;

        public NifObjectBuilder(NiFile file, MaterialManager materialManager)
        {
            _file = file;
            //TODO replace this with DI or something
            _modelDelegates = new List<INiObjectDelegate>
            {
                new NiNodeDelegate(),
                new NiTriShapeDelegate(materialManager)
            };
            _collisionDelegates = new List<INiObjectDelegate>
            {
                new BhkCollisionObjectDelegate(),
                new BhkCompressedMeshShapeDataDelegate(),
                new BhkCompressedMeshShapeDelegate(),
                new BhkConvexVerticesShapeDelegate(),
                new BhkListShapeDelegate(),
                new BhkRigidBodyDelegate(),
                new BvTreeShapeDelegate(),
            };
        }

        public IEnumerator BuildObject(Action<GameObject> onReadyCallback)
        {
            if (_file == null)
            {
                onReadyCallback(null);
                yield break;
            }

            Debug.Assert((_file.Name != null) && (_file.Footer.RootReferences.Length > 0));

            if (_file.Footer.RootReferences.Length == 1)
            {
                var rootNiObject = _file.NiObjects[_file.Footer.RootReferences[0]];

                var gameObjectCoroutine = InstantiateNiObject(rootNiObject,
                    gameObject =>
                    {
                        if (gameObject == null)
                        {
                            Debug.Log(_file.Name + " resulted in a null GameObject when instantiated.");

                            gameObject = new GameObject(_file.Name);
                        }
                        else if (rootNiObject is NiNode)
                        {
                            gameObject.transform.position = Vector3.zero;
                            gameObject.transform.rotation = Quaternion.identity;
                            gameObject.transform.localScale = Vector3.one;
                        }

                        onReadyCallback(gameObject);
                    });
                if (gameObjectCoroutine == null)
                {
                    onReadyCallback(null);
                    yield break;
                }

                while (gameObjectCoroutine.MoveNext())
                {
                    yield return null;
                }
            }
            else
            {
                var gameObject = new GameObject(_file.Name);

                foreach (var rootRef in _file.Footer.RootReferences)
                {
                    var rootBlock = _file.NiObjects[rootRef];
                    var childCoroutine = InstantiateNiObject(rootBlock, child =>
                    {
                        if (child != null)
                        {
                            child.transform.SetParent(gameObject.transform, false);
                        }
                    });

                    if (childCoroutine == null) continue;
                    while (childCoroutine.MoveNext())
                    {
                        yield return null;
                    }
                }

                onReadyCallback(gameObject);
            }
        }

        private IEnumerator InstantiateNiObject(NiObject niObject, Action<GameObject> onReadyCallback)
        {
            GameObject gameObject = null;
            var objectDelegate = _modelDelegates.FirstOrDefault(modelDelegate => modelDelegate.IsApplicable(niObject));
            if (objectDelegate == null)
            {
                Debug.LogWarning($"No delegate found for {niObject.GetType().Name}");
                onReadyCallback(null);
                yield break;
            }

            var enumerator = objectDelegate.Instantiate(_file, niObject, InstantiateNiObject,
                gameObj => { gameObject = gameObj; });
            if (enumerator == null)
            {
                onReadyCallback(null);
                yield break;
            }

            while (enumerator.MoveNext())
            {
                yield return null;
            }

            if (gameObject == null)
            {
                onReadyCallback(null);
                yield break;
            }

            if (niObject is not NiAvObject { CollisionObjectReference: > 0 } anNiAvObject)
            {
                onReadyCallback(gameObject);
                yield break;
            }

            var collisionInfo = _file.NiObjects[anNiAvObject.CollisionObjectReference];
            var collisionObjectEnumerator = InstantiateCollisionObject(
                collisionInfo,
                collisionObject =>
                {
                    if (collisionObject != null)
                    {
                        collisionObject.transform.SetParent(gameObject.transform, false);
                    }
                });
            if (collisionObjectEnumerator == null)
            {
                onReadyCallback(gameObject);
                yield break;
            }

            while (collisionObjectEnumerator.MoveNext())
            {
                yield return null;
            }

            onReadyCallback(gameObject);
        }

        private IEnumerator InstantiateCollisionObject(NiObject collisionObj, Action<GameObject> onReadyCallback)
        {
            var collisionDelegate = _collisionDelegates.FirstOrDefault(collisionDelegate => collisionDelegate.IsApplicable(collisionObj));
            if (collisionDelegate != null)
                return collisionDelegate.Instantiate(_file, collisionObj, InstantiateCollisionObject, onReadyCallback);
            Debug.LogWarning($"No delegate found for {collisionObj.GetType().Name}");
            return null;
        }
    }
}