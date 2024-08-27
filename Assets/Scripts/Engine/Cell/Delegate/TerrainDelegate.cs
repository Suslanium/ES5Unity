using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Engine.Cell.Delegate.Interfaces;
using Engine.Core;
using Engine.MasterFile;
using Engine.Textures;
using JetBrains.Annotations;
using MasterFile.MasterFileContents;
using MasterFile.MasterFileContents.Records;
using UnityEngine;
using Convert = Engine.Core.Convert;

namespace Engine.Cell.Delegate
{
    public enum Quadrant
    {
        BottomLeft = 0,
        BottomRight = 1,
        TopLeft = 2,
        TopRight = 3
    }

    public readonly struct TerrainMeshInfo
    {
        public readonly Vector3 Size;

        [CanBeNull] public readonly float[,] HeightMap;

        public readonly float MinHeight;

        public readonly float MaxHeight;

        public TerrainMeshInfo(Vector3 size, [CanBeNull] float[,] heightMap, float minHeight, float maxHeight)
        {
            Size = size;
            HeightMap = heightMap;
            MinHeight = minHeight;
            MaxHeight = maxHeight;
        }
    }

    public readonly struct TerrainLayerInfo
    {
        public readonly uint TextureFormID;

        public readonly Quadrant Quadrant;

        //Null alphamap = covers the entire quadrant
        [CanBeNull] public readonly float[,] AlphaMap;

        public TerrainLayerInfo(uint textureFormID, Quadrant quadrant, [CanBeNull] float[,] alphaMap)
        {
            TextureFormID = textureFormID;
            Quadrant = quadrant;
            AlphaMap = alphaMap;
        }
    }

    public readonly struct MergedTerrainLayerInfo
    {
        public readonly uint TextureFormID;

        //Null list = covers the entire quadrant
        public readonly Dictionary<Quadrant, List<float[,]>> QuadrantAlphaMaps;

        public MergedTerrainLayerInfo(uint textureFormID, Dictionary<Quadrant, List<float[,]>> quadrantAlphaMaps)
        {
            TextureFormID = textureFormID;
            QuadrantAlphaMaps = quadrantAlphaMaps;
        }
    }

    public readonly struct LoadedTerrainLayersInfo
    {
        public readonly uint[] TextureFormIDs;

        public readonly float[,,] AlphaMaps;

        public LoadedTerrainLayersInfo(uint[] textureFormIDs, float[,,] alphaMaps)
        {
            TextureFormIDs = textureFormIDs;
            AlphaMaps = alphaMaps;
        }
    }

    public class TerrainDelegate : CellRecordPreprocessDelegate<LAND>, ICellRecordInstantiationDelegate,
        ICellDestroyDelegate
    {
        private const int AlphaMapResolution = 128;
        private const int TerrainQuadrantResolution = AlphaMapResolution / 2;
        private const int QuadrantRawAlphaMapResolution = Convert.ExteriorCellQuadrantSideLengthInSamples;
        private const int LandSideLength = Convert.ExteriorCellSideLengthInSamples;
        private const string TexturePathPrefix = "Textures";
        private const string DefaultDiffuseTexturePath = "Textures/Landscape/Dirt02.dds";
        private const string DefaultNormalMapPath = "Textures/Landscape/Dirt02_n.dds";
        private const string DefaultTerrainShader = "Nature/Terrain/Diffuse";

        private readonly Dictionary<uint, (Texture2D, Texture2D)> _textureCache = new();
        private readonly ConcurrentDictionary<uint, (string, string)> _texturePaths = new();
        private readonly Dictionary<uint, Task> _textureLoadingTasks = new();

        private readonly Dictionary<uint, Task<TerrainMeshInfo>> _terrainMeshInfoTasks = new();
        private readonly Dictionary<uint, TerrainMeshInfo> _terrainMeshInfos = new();

        private readonly Dictionary<uint, Task<LoadedTerrainLayersInfo>> _terrainLayerTasks = new();
        private readonly Dictionary<uint, LoadedTerrainLayersInfo> _terrainLayers = new();

        private readonly MasterFileManager _masterFileManager;
        private readonly TextureManager _textureManager;

        public TerrainDelegate(MasterFileManager masterFileManager, TextureManager textureManager)
        {
            _masterFileManager = masterFileManager;
            _textureManager = textureManager;
        }

        private static Task<TerrainMeshInfo> GenerateTerrainMeshInfo(float[,] heightMap)
        {
            return Task.Run(() =>
            {
                Utils.GetExtrema(heightMap, out var minHeight, out var maxHeight);

                for (var y = 0; y < LandSideLength; y++)
                {
                    for (var x = 0; x < LandSideLength; x++)
                    {
                        heightMap[y, x] = Utils.ChangeRange(heightMap[y, x], minHeight, maxHeight, 0, 1);
                    }
                }

                var heightRange = maxHeight - minHeight;
                var maxHeightInMeters = heightRange / Convert.meterInMWUnits;

                const float terrainWidth = (LandSideLength - 1) *
                                           (Convert.ExteriorCellSideLengthInMeters / (LandSideLength - 1));

                return !Mathf.Approximately(maxHeightInMeters, 0)
                    ? new TerrainMeshInfo(new Vector3(terrainWidth, maxHeightInMeters, terrainWidth), heightMap,
                        minHeight, maxHeight)
                    : new TerrainMeshInfo(new Vector3(terrainWidth, 1, terrainWidth), null, minHeight, maxHeight);
            });
        }

        private static IEnumerator<List<List<TerrainLayerInfo>>> GetTerrainLayerInfo(LAND record)
        {
            var terrainLayersInfo = new Dictionary<Quadrant, List<TerrainLayerInfo>>();

            var missingQuadrants = new HashSet<Quadrant>
                { Quadrant.TopLeft, Quadrant.TopRight, Quadrant.BottomRight, Quadrant.BottomLeft };
            foreach (var baseTexture in record.BaseTextures)
            {
                var quadrant = (Quadrant)baseTexture.Quadrant;
                terrainLayersInfo[quadrant] = new List<TerrainLayerInfo>
                {
                    new(baseTexture.LandTextureFormID,
                        quadrant, null)
                };
                missingQuadrants.Remove(quadrant);
                yield return null;
            }

            foreach (var missingQuadrant in missingQuadrants)
            {
                terrainLayersInfo[missingQuadrant] = new List<TerrainLayerInfo>
                {
                    new(0, missingQuadrant, null)
                };
                yield return null;
            }

            foreach (var additionalTexture in record.AdditionalTextures)
            {
                var quadrant = (Quadrant)additionalTexture.Quadrant;

                terrainLayersInfo[quadrant].Add(new TerrainLayerInfo(additionalTexture.LandTextureFormID,
                    quadrant, additionalTexture.QuadrantAlphaMap));
                yield return null;
            }

            yield return terrainLayersInfo.Values.ToList();
        }

        private static Task<LoadedTerrainLayersInfo> LoadTerrainLayersMinimized(
            List<List<TerrainLayerInfo>> quadrantLayers)
        {
            return Task.Run(() =>
            {
                var quadrantLayerAmount = quadrantLayers.Select(list => list.Count).ToArray();
                const int indexLimit = 10;
                var memo = new Dictionary<(int, int, int, int), int>();
                //Self-managed stack to avoid potential stack overflow
                var stack = new Stack<(int, int, int, int, List<MergedTerrainLayerInfo>, int)>();
                var bestResultListSize = int.MaxValue;
                List<MergedTerrainLayerInfo> bestResultList = null;
                stack.Push((0, 0, 0, 0, new List<MergedTerrainLayerInfo>(), 0));

                while (stack.Count > 0)
                {
                    var (i0, i1, i2, i3, currentList, currentSize) = stack.Pop();

                    if (i0 >= indexLimit || i1 >= indexLimit || i2 >= indexLimit || i3 >= indexLimit)
                        continue;

                    if (i0 >= quadrantLayerAmount[0] && i1 >= quadrantLayerAmount[1] && i2 >= quadrantLayerAmount[2] &&
                        i3 >= quadrantLayerAmount[3])
                    {
                        if (currentSize < bestResultListSize)
                        {
                            bestResultListSize = currentSize;
                            bestResultList = currentList;
                        }

                        continue;
                    }

                    if (!memo.TryGetValue((i0, i1, i2, i3), out var memoizedListSize))
                    {
                        memo[(i0, i1, i2, i3)] = currentSize;
                    }
                    else
                    {
                        if (memoizedListSize <= currentSize) continue;
                        memo[(i0, i1, i2, i3)] = currentSize;
                    }

                    var layers = new List<TerrainLayerInfo>();
                    if (i0 < quadrantLayerAmount[0])
                        layers.Add(quadrantLayers[0][i0]);
                    if (i1 < quadrantLayerAmount[1])
                        layers.Add(quadrantLayers[1][i1]);
                    if (i2 < quadrantLayerAmount[2])
                        layers.Add(quadrantLayers[2][i2]);
                    if (i3 < quadrantLayerAmount[3])
                        layers.Add(quadrantLayers[3][i3]);

                    var mergedLayers = new Dictionary<uint, Dictionary<Quadrant, List<float[,]>>>();
                    foreach (var layer in layers)
                    {
                        if (!mergedLayers.ContainsKey(layer.TextureFormID))
                        {
                            mergedLayers[layer.TextureFormID] = new Dictionary<Quadrant, List<float[,]>>();
                        }

                        if (!mergedLayers[layer.TextureFormID].ContainsKey(layer.Quadrant))
                        {
                            mergedLayers[layer.TextureFormID][layer.Quadrant] = layer.AlphaMap == null
                                ? null
                                : new List<float[,]>
                                {
                                    layer.AlphaMap
                                };
                        }
                        else
                        {
                            if (layer.AlphaMap == null)
                            {
                                mergedLayers[layer.TextureFormID][layer.Quadrant] = null;
                            }
                            else
                            {
                                mergedLayers[layer.TextureFormID][layer.Quadrant]?.Add(layer.AlphaMap);
                            }
                        }
                    }

                    var currentMergedLayers =
                        mergedLayers.Select(layer => new MergedTerrainLayerInfo(layer.Key, layer.Value));

                    var newCurrentList = new List<MergedTerrainLayerInfo>(currentList);
                    newCurrentList.AddRange(currentMergedLayers);

                    for (var advance = 0; advance < 16; advance++)
                    {
                        var ni0 = i0 + ((advance & 1) > 0 ? 1 : 0);
                        var ni1 = i1 + ((advance & 2) > 0 ? 1 : 0);
                        var ni2 = i2 + ((advance & 4) > 0 ? 1 : 0);
                        var ni3 = i3 + ((advance & 8) > 0 ? 1 : 0);
                        stack.Push((ni0, ni1, ni2, ni3, newCurrentList, newCurrentList.Count));
                    }
                }

                if (bestResultList == null)
                    return new LoadedTerrainLayersInfo(Array.Empty<uint>(), new float[,,] { });

                var mergedAlphaMaps = new float[AlphaMapResolution, AlphaMapResolution, bestResultList.Count];
                var textureFormIDs = new uint[bestResultList.Count];

                //Merge the alphamaps in the best result list
                var tasks = new Task[bestResultList.Count - 1];

                for (var i = 0; i < bestResultList.Count - 1; i++)
                {
                    //Start tasks for merging each resulting layer except the last one
                    var currentLayer = bestResultList[i];
                    var currentLayerIndex = i;
                    tasks[i] = Task.Run(() =>
                        WriteMergedTerrainLayer(currentLayer, textureFormIDs, mergedAlphaMaps, currentLayerIndex));
                }

                //Merge the last layer on the current thread to avoid wasting resources
                WriteMergedTerrainLayer(bestResultList[^1], textureFormIDs, mergedAlphaMaps, bestResultList.Count - 1);

                Task.WaitAll(tasks);

                return new LoadedTerrainLayersInfo(textureFormIDs, mergedAlphaMaps);
            });
        }

        private static Quadrant GetQuadrant(int x, int y)
        {
            if (x < AlphaMapResolution / 2 && y < AlphaMapResolution / 2)
                return Quadrant.BottomLeft;
            if (x >= AlphaMapResolution / 2 && y < AlphaMapResolution / 2)
                return Quadrant.TopLeft;
            if (x < AlphaMapResolution / 2 && y >= AlphaMapResolution / 2)
                return Quadrant.BottomRight;
            if (x >= AlphaMapResolution / 2 && y >= AlphaMapResolution / 2)
                return Quadrant.TopRight;
            throw new ArgumentException("Invalid coordinates for alpha map quadrant determination");
        }

        private static void WriteMergedTerrainLayer(MergedTerrainLayerInfo mergedTerrainLayerInfo,
            uint[] textureFormIDs,
            float[,,] resultingAlphaMaps, int layerIndex)
        {
            for (var y = 0; y < AlphaMapResolution; y++)
            {
                for (var x = 0; x < AlphaMapResolution; x++)
                {
                    //Determine the current quadrant
                    var quadrant = GetQuadrant(x, y);
                    //Get the alpha maps for the current quadrant
                    if (!mergedTerrainLayerInfo.QuadrantAlphaMaps.TryGetValue(quadrant, out var alphaMaps))
                        continue;

                    if (alphaMaps == null)
                    {
                        //Cover the entire quadrant
                        resultingAlphaMaps[x, y, layerIndex] = 1;
                        continue;
                    }

                    //Get raw alpha map coordinates
                    var qx = (float)(x % TerrainQuadrantResolution) / TerrainQuadrantResolution *
                             QuadrantRawAlphaMapResolution;
                    var qy = (float)(y % TerrainQuadrantResolution) / TerrainQuadrantResolution *
                             QuadrantRawAlphaMapResolution;

                    var xLess = Mathf.Max(0, Mathf.FloorToInt(qx));
                    var xMore = Mathf.Min(QuadrantRawAlphaMapResolution - 1, Mathf.CeilToInt(qx));
                    var xFractional = qx - xLess;

                    var yLess = Mathf.Max(0, Mathf.FloorToInt(qy));
                    var yMore = Mathf.Min(QuadrantRawAlphaMapResolution - 1, Mathf.CeilToInt(qy));
                    var yFractional = qy - yLess;

                    var topLeftWeight = (1 - xFractional) * (1 - yFractional);
                    var topRightWeight = xFractional * (1 - yFractional);
                    var bottomLeftWeight = (1 - xFractional) * yFractional;
                    var bottomRightWeight = xFractional * yFractional;

                    float valueSum = 0;
                    foreach (var alphaMap in alphaMaps)
                    {
                        var topLeft = alphaMap[xLess, yLess];
                        var topRight = alphaMap[xMore, yLess];
                        var bottomLeft = alphaMap[xLess, yMore];
                        var bottomRight = alphaMap[xMore, yMore];
                        valueSum += topLeft * topLeftWeight + topRight * topRightWeight +
                                    bottomLeft * bottomLeftWeight + bottomRight * bottomRightWeight;
                    }

                    resultingAlphaMaps[x, y, layerIndex] = Mathf.Clamp01(valueSum);
                }
            }

            textureFormIDs[layerIndex] = mergedTerrainLayerInfo.TextureFormID;
        }

        private (string, string) GetTexturePaths(uint landTextureFormID)
        {
            if (landTextureFormID == 0)
                return (DefaultDiffuseTexturePath, DefaultNormalMapPath);

            var landTextureRecord = _masterFileManager.GetFromFormID(landTextureFormID);

            if (landTextureRecord is not LTEX { TextureFormID: not null } ltex)
                return (DefaultDiffuseTexturePath, DefaultNormalMapPath);

            var textureRecord = _masterFileManager.GetFromFormID(ltex.TextureFormID.Value);

            if (textureRecord is not TXST texture)
                return (DefaultDiffuseTexturePath, DefaultNormalMapPath);

            var diffuseMapPath = texture.DiffuseMapPath;
            var normalMapPath = texture.NormalMapPath;

            if (string.IsNullOrEmpty(diffuseMapPath))
                return (DefaultDiffuseTexturePath, DefaultNormalMapPath);

            if (!diffuseMapPath.StartsWith(TexturePathPrefix, ignoreCase: true, CultureInfo.InvariantCulture))
                diffuseMapPath = $"{TexturePathPrefix}/{diffuseMapPath}";
            diffuseMapPath = diffuseMapPath.Replace('\\', '/');

            if (normalMapPath != null &&
                !normalMapPath.StartsWith(TexturePathPrefix, ignoreCase: true, CultureInfo.InvariantCulture))
                normalMapPath = $"{TexturePathPrefix}/{normalMapPath}";
            normalMapPath = normalMapPath?.Replace('\\', '/');

            return (diffuseMapPath, normalMapPath);
        }

        private Task PreloadTexturesAndSavePaths(uint landTextureFormID)
        {
            return Task.Run(() =>
            {
                var (diffuseMapPath, normalMapPath) = GetTexturePaths(landTextureFormID);
                _texturePaths[landTextureFormID] = (diffuseMapPath, normalMapPath);

                if (diffuseMapPath != null)
                    _textureManager.PreloadMap(TextureType.DIFFUSE, diffuseMapPath);
                if (normalMapPath != null)
                    _textureManager.PreloadMap(TextureType.NORMAL, normalMapPath);
            });
        }

        //Start preloading the terrain stuff
        protected override IEnumerator PreprocessRecord(CELL cell, LAND record, GameObject parent)
        {
            yield return null;
            var layersCoroutine = GetTerrainLayerInfo(record);
            while (layersCoroutine.MoveNext())
                yield return null;
            var layers = layersCoroutine.Current;
            if (layers == null)
            {
                Debug.LogError($"Failed to get terrain layer info for record {record.FormID}");
                yield break;
            }

            foreach (var layerList in layers)
            {
                foreach (var layer in layerList)
                {
                    if (!_textureLoadingTasks.ContainsKey(layer.TextureFormID))
                    {
                        _textureLoadingTasks[layer.TextureFormID] =
                            PreloadTexturesAndSavePaths(layer.TextureFormID);
                    }

                    yield return null;
                }
            }

            if (!_terrainMeshInfoTasks.ContainsKey(record.FormID))
            {
                _terrainMeshInfoTasks[record.FormID] =
                    GenerateTerrainMeshInfo(record.VertexHeightMap);
                yield return null;
            }

            if (!_terrainLayerTasks.ContainsKey(record.FormID))
            {
                _terrainLayerTasks[record.FormID] = LoadTerrainLayersMinimized(layers);
                yield return null;
            }
        }

        //Build the terrain from the preloaded stuff
        public IEnumerator InstantiateRecord(CELL cell, Record record, GameObject parent)
        {
            if (record is not LAND land)
                yield break;

            if (!_terrainMeshInfos.TryGetValue(land.FormID, out var meshInfo))
            {
                yield return null;
                if (!_terrainMeshInfoTasks.TryGetValue(land.FormID, out var meshInfoTask))
                {
                    Debug.LogError($"Terrain mesh info task not found for record {land.FormID}");
                    yield break;
                }

                yield return null;

                while (!meshInfoTask.IsCompleted)
                    yield return null;

                meshInfo = meshInfoTask.Result;
                yield return null;

                _terrainMeshInfos[land.FormID] = meshInfo;
            }

            var terrainData = new TerrainData
            {
                heightmapResolution = LandSideLength,
                alphamapResolution = AlphaMapResolution,
                size = meshInfo.Size
            };
            yield return null;

            if (meshInfo.HeightMap != null)
                terrainData.SetHeights(0, 0, meshInfo.HeightMap);
            yield return null;

            if (!_terrainLayers.TryGetValue(land.FormID, out var layersInfo))
            {
                yield return null;
                if (!_terrainLayerTasks.TryGetValue(land.FormID, out var layersInfoTask))
                {
                    Debug.LogError($"Terrain layers info task not found for record {land.FormID}");
                    yield break;
                }

                yield return null;

                while (!layersInfoTask.IsCompleted)
                    yield return null;

                layersInfo = layersInfoTask.Result;
                yield return null;

                _terrainLayers[land.FormID] = layersInfo;
            }

            var terrainLayers = new TerrainLayer[layersInfo.TextureFormIDs.Length];
            yield return null;
            for (var i = 0; i < layersInfo.TextureFormIDs.Length; i++)
            {
                var textureFormID = layersInfo.TextureFormIDs[i];
                if (!_textureCache.TryGetValue(textureFormID, out var textures))
                {
                    yield return null;
                    if (!_textureLoadingTasks.TryGetValue(textureFormID, out var task))
                    {
                        Debug.LogError($"Texture loading task not found for texture form ID {textureFormID}");
                        continue;
                    }

                    yield return null;

                    while (!task.IsCompleted)
                        yield return null;

                    if (!_texturePaths.TryGetValue(textureFormID, out var texturePaths))
                    {
                        Debug.LogError($"Texture paths not found for texture form ID {textureFormID}");
                        continue;
                    }

                    yield return null;

                    var diffuseMapPath = texturePaths.Item1;
                    var normalMapPath = texturePaths.Item2;

                    Texture2D diffuseMap = null;
                    Texture2D normalMap = null;

                    var diffuseMapCoroutine = _textureManager.GetMap<Texture2D>(TextureType.DIFFUSE, diffuseMapPath);
                    if (diffuseMapCoroutine != null)
                    {
                        while (diffuseMapCoroutine.MoveNext())
                            yield return null;
                        diffuseMap = diffuseMapCoroutine.Current;
                        yield return null;
                    }

                    var normalMapCoroutine =
                        _textureManager.GetMap<Texture2D>(TextureType.NORMAL, normalMapPath);
                    if (normalMapCoroutine != null)
                    {
                        while (normalMapCoroutine.MoveNext())
                            yield return null;
                        normalMap = normalMapCoroutine.Current;
                        yield return null;
                    }

                    textures = (diffuseMap, normalMap);
                    yield return null;
                    _textureCache[textureFormID] = textures;
                    yield return null;
                }

                var layer = new TerrainLayer
                {
                    diffuseTexture = textures.Item1,
                    normalMapTexture = textures.Item2,
                    tileOffset = Vector2.zero,
                    tileSize = Vector2.one * 2
                };
                yield return null;
                terrainLayers[i] = layer;
            }

            yield return null;
            terrainData.terrainLayers = terrainLayers;
            yield return null;
            terrainData.SetAlphamaps(0, 0, layersInfo.AlphaMaps);
            yield return null;

            var gameObject = new GameObject("terrain");
            yield return null;

            var terrain = gameObject.AddComponent<Terrain>();
            yield return null;
            terrain.terrainData = terrainData;
            yield return null;
            terrain.materialTemplate = new Material(Shader.Find(DefaultTerrainShader));
            yield return null;

            gameObject.AddComponent<TerrainCollider>().terrainData = terrainData;
            yield return null;

            var terrainPosition = new Vector3(Convert.ExteriorCellSideLengthInMeters * cell.XGridPosition,
                meshInfo.MinHeight / Convert.meterInMWUnits,
                Convert.ExteriorCellSideLengthInMeters * cell.YGridPosition);
            yield return null;

            gameObject.transform.position = terrainPosition;
            yield return null;

            gameObject.transform.parent = parent.transform;
            yield return null;
        }

        public IEnumerator OnDestroy()
        {
            _textureCache.Clear();
            yield return null;
            _texturePaths.Clear();
            yield return null;
            _textureLoadingTasks.Clear();
            yield return null;
            _terrainMeshInfoTasks.Clear();
            yield return null;
            _terrainMeshInfos.Clear();
            yield return null;
            _terrainLayerTasks.Clear();
            yield return null;
            _terrainLayers.Clear();
            yield return null;
        }
    }
}