using NIF.NiObjects;
using UnityEngine;

namespace NIF.Converter
{
    /// <summary>
    /// Based on https://github.com/ColeDeanShepherd/TESUnity/blob/f4d5e19f68da380da9da745356c7904f3428b9d6/Assets/Scripts/TES/NIF/NIFObjectBuilder.cs
    /// </summary>
    public class NIFObjectBuilder
    {
        private NIFile file;

        public NIFObjectBuilder(NIFile file)
        {
            this.file = file;
        }

        public GameObject BuildObject()
        {
            Debug.Assert((file.Name != null) && (file.Footer.RootReferences.Length > 0));

            if (file.Footer.RootReferences.Length == 1)
            {
                var rootNiObject = file.NiObjects[file.Footer.RootReferences[0]];

                GameObject gameObject = InstantiateRootNiObject(rootNiObject);

                if (gameObject == null)
                {
                    Debug.Log(file.Name + " resulted in a null GameObject when instantiated.");

                    gameObject = new GameObject(file.Name);
                }
                else if (rootNiObject is NiNode)
                {
                    gameObject.transform.position = Vector3.zero;
                    gameObject.transform.rotation = Quaternion.identity;
                    gameObject.transform.localScale = Vector3.one;
                }

                return gameObject;
            }
            else
            {
                GameObject gameObject = new GameObject(file.Name);

                foreach (var rootRef in file.Footer.RootReferences)
                {
                    var rootBlock = file.NiObjects[rootRef];
                    var child = InstantiateRootNiObject(rootBlock);

                    if (child != null)
                    {
                        child.transform.SetParent(gameObject.transform, false);
                    }
                }

                return gameObject;
            }
        }

        private GameObject InstantiateRootNiObject(NiObject niObject)
        {
            var gameObject = InstantiateNiObject(niObject);

            //Additional processing

            return gameObject;
        }

        private GameObject InstantiateNiObject(NiObject niObject)
        {
            return niObject switch
            {
                NiNode node => InstantiateNiNode(node),
                NiTriShape shape => InstantiateNiTriShape(shape),
                BSLodTriShape shape => InstantiateNiTriShape(shape),
                _ => null
            };
        }

        private GameObject InstantiateNiNode(NiNode node)
        {
            GameObject gameObject = new GameObject(node.Name);

            foreach (var childRef in node.ChildrenReferences)
            {
                if (childRef < 0) continue;
                var child = InstantiateNiObject(file.NiObjects[childRef]);

                if (child != null)
                {
                    child.transform.SetParent(gameObject.transform, false);
                }
            }

            ApplyNiAVObject(node, gameObject);

            return gameObject;
        }

        private GameObject InstantiateNiTriShape(NiTriBasedGeom triShape)
        {
            var mesh = NiTriShapeDataToMesh((NiTriShapeData)file.NiObjects[triShape.DataReference]);
            var gameObject = new GameObject(triShape.Name);

            gameObject.AddComponent<MeshFilter>().mesh = mesh;
            var meshRenderer = gameObject.AddComponent<MeshRenderer>();
            
            ApplyNiAVObject(triShape, gameObject);
            return gameObject;
        }

        private Mesh NiTriShapeDataToMesh(NiTriShapeData data)
        {
            Vector3[] vertices = null;
            if (data.HasVertices)
            {
                vertices = new Vector3[data.Vertices.Length];
                for (var i = 0; i < vertices.Length; i++)
                {
                    vertices[i] = NIFUtils.NifPointToUnityPoint(data.Vertices[i].ToUnityVector());
                }
            }

            Vector3[] normals = null;
            if (data.HasNormals)
            {
                normals = new Vector3[data.Normals.Length];
                for (var i = 0; i < normals.Length; i++)
                {
                    normals[i] = NIFUtils.NifPointToUnityPoint(data.Normals[i].ToUnityVector());
                }
            }

            Vector4[] tangents = null;
            if (data.Tangents != null)
            {
                tangents = new Vector4[data.Tangents.Length];
                for (var i = 0; i < tangents.Length; i++)
                {
                    var convertedTangent = NIFUtils.NifPointToUnityPoint(data.Tangents[i].ToUnityVector());
                    tangents[i] = new Vector4(convertedTangent.x, convertedTangent.y, convertedTangent.z, 1);
                }
            }

            Vector2[] UVs = null;
            if (data.UVSets != null && vertices != null)
            {
                UVs = new Vector2[vertices.Length];

                for (var i = 0; i < UVs.Length; i++)
                {
                    var texCoord = data.UVSets[0, i];

                    UVs[i] = new Vector2(texCoord.U, texCoord.V);
                }
            }

            int[] triangles = null;
            if (data.HasTriangles)
            {
                triangles = new int[data.TrianglePointsNumber];
                for (var i = 0; i < data.Triangles.Length; i++)
                {
                    var baseI = 3 * i;

                    triangles[baseI] = data.Triangles[i].V1;
                    triangles[baseI + 1] = data.Triangles[i].V3;
                    triangles[baseI + 2] = data.Triangles[i].V2;
                }
            }

            var mesh = new Mesh
            {
                vertices = vertices,
                normals = normals,
                tangents = tangents,
                uv = UVs,
                triangles = triangles
            };

            if (!data.HasNormals)
            {
                mesh.RecalculateNormals();
            }

            mesh.RecalculateBounds();

            return mesh;
        }

        private void ApplyNiAVObject(NiAVObject anNiAVObject, GameObject obj)
        {
            obj.transform.position = NIFUtils.NifPointToUnityPoint(anNiAVObject.Translation.ToUnityVector());
            obj.transform.rotation = NIFUtils.NifRotationMatrixToUnityQuaternion(anNiAVObject.Rotation.ToMatrix4x4());
            obj.transform.localScale = anNiAVObject.Scale * Vector3.one;
        }
    }
}