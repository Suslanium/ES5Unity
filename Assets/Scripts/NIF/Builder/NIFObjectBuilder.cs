using System.Collections.Generic;
using System.Linq;
using Engine.Textures;
using NIF.Builder.Delegate;
using NIF.Builder.Delegate.Collision;
using NIF.Parser;
using NIF.Parser.NiObjects;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;
using GameObject = NIF.Builder.Components.GameObject;
using Logger = Engine.Core.Logger;

namespace NIF.Builder
{
    /// <summary>
    /// Based on https://github.com/ColeDeanShepherd/TESUnity/blob/f4d5e19f68da380da9da745356c7904f3428b9d6/Assets/Scripts/TES/NIF/NIFObjectBuilder.cs
    /// </summary>
    public class NifObjectBuilder
    {
        private readonly NiFile _file;
        private static List<INiObjectDelegate> _modelDelegates;
        private static List<INiObjectDelegate> _collisionDelegates;

        public static void Initialize(MaterialManager materialManager, TextureManager textureManager)
        {
            //TODO replace this with DI or something
            _modelDelegates ??= new List<INiObjectDelegate>
            {
                new NiNodeDelegate(),
                new NiTriShapeDelegate(materialManager, textureManager)
            };
            _collisionDelegates ??= new List<INiObjectDelegate>
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

        public NifObjectBuilder(NiFile file)
        {
            _file = file;
        }

        public GameObject BuildObject()
        {
            if (_file == null)
            {
                return null;
            }

            Debug.Assert(_file.Name != null && _file.Footer.RootReferences.Length > 0);

            if (_file.Footer.RootReferences.Length == 1)
            {
                var rootNiObject = _file.NiObjects[_file.Footer.RootReferences[0]];

                var gameObject = InstantiateNiObject(rootNiObject);

                if (gameObject == null)
                {
                    Logger.Log(_file.Name + " resulted in a null GameObject when instantiated.");

                    gameObject = new GameObject(_file.Name);
                }
                else if (rootNiObject is NiNode)
                {
                    gameObject.Position = Vector3.zero;
                    gameObject.Rotation = Quaternion.identity;
                    gameObject.Scale = Vector3.one;
                }

                return gameObject;
            }
            else
            {
                var gameObject = new GameObject(_file.Name);

                foreach (var rootRef in _file.Footer.RootReferences)
                {
                    var rootBlock = _file.NiObjects[rootRef];
                    var child = InstantiateNiObject(rootBlock);
                    if (child != null)
                    {
                        child.Parent = gameObject;
                    }
                }

                return gameObject;
            }
        }

        private GameObject InstantiateNiObject(NiObject niObject)
        {
            var objectDelegate = _modelDelegates.FirstOrDefault(modelDelegate => modelDelegate.IsApplicable(niObject));
            if (objectDelegate == null)
            {
                Logger.LogWarning($"No delegate found for {niObject.GetType().Name}");
                return null;
            }

            var gameObject = objectDelegate.Instantiate(_file, niObject, InstantiateNiObject);
            if (gameObject == null)
            {
                return null;
            }

            if (niObject is not NiAvObject { CollisionObjectReference: > 0 } anNiAvObject)
            {
                return gameObject;
            }

            var collisionInfo = _file.NiObjects[anNiAvObject.CollisionObjectReference];
            var collisionObject = InstantiateCollisionObject(collisionInfo);
            if (collisionObject != null)
            {
                collisionObject.Parent = gameObject;
            }

            return gameObject;
        }

        private GameObject InstantiateCollisionObject(NiObject collisionObj)
        {
            var collisionDelegate =
                _collisionDelegates.FirstOrDefault(collisionDelegate => collisionDelegate.IsApplicable(collisionObj));
            if (collisionDelegate != null)
                return collisionDelegate.Instantiate(_file, collisionObj, InstantiateCollisionObject);
            Logger.LogWarning($"No delegate found for {collisionObj.GetType().Name}");
            return null;
        }
    }
}