using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Engine.Cell.Delegate.Interfaces;
using Engine.Core;
using Engine.MasterFile;
using Engine.Textures;
using JetBrains.Annotations;
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

    public struct TerrainTexture
    {
        public readonly Texture2D DiffuseTexture;
        [CanBeNull] public readonly Texture2D NormalMap;
        public readonly float[,] AlphaMap;

        public TerrainTexture(Texture2D diffuseTexture, [CanBeNull] Texture2D normalMap, float[,] alphaMap)
        {
            AlphaMap = alphaMap;
            DiffuseTexture = diffuseTexture;
            NormalMap = normalMap;
        }
    }

    public struct NonLoadedTerrainLayer
    {
        public readonly string DiffuseMapPath;
        [CanBeNull] public readonly string NormalMapPath;
        public readonly float[,] AlphaMap;

        public NonLoadedTerrainLayer(string diffuseMapPath, [CanBeNull] string normalMapPath, float[,] alphaMap)
        {
            DiffuseMapPath = diffuseMapPath;
            NormalMapPath = normalMapPath;
            AlphaMap = alphaMap;
        }
    }

    public readonly struct MergedLayer
    {
        public readonly string DiffuseMapPath;
        [CanBeNull] public readonly string NormalMapPath;
        public readonly List<int> AlphaMapIndices;

        public MergedLayer(string diffuseMapPath, [CanBeNull] string normalMapPath, int alphaMapIndex)
        {
            DiffuseMapPath = diffuseMapPath;
            NormalMapPath = normalMapPath;
            AlphaMapIndices = new List<int> { alphaMapIndex };
        }

        public MergedLayer(string diffuseMapPath, [CanBeNull] string normalMapPath, List<int> alphaMapIndices)
        {
            DiffuseMapPath = diffuseMapPath;
            NormalMapPath = normalMapPath;
            AlphaMapIndices = alphaMapIndices;
        }
    }

    public class TerrainDelegate : CellRecordPreprocessDelegate<LAND>
    {
        private const int LandSideLength = Convert.ExteriorCellSideLengthInSamples;
        private const string TexturePathPrefix = "Textures";
        private static readonly string DefaultDiffuseTexturePath = @"Textures\Landscape\Dirt02.dds";
        private static readonly string DefaultNormalMapPath = @"Textures\Landscape\Dirt02_n.dds";
        private const string DefaultTerrainShader = "Nature/Terrain/Diffuse";

        private readonly MasterFileManager _masterFileManager;
        private readonly TextureManager _textureManager;

        public TerrainDelegate(MasterFileManager masterFileManager, TextureManager textureManager)
        {
            _masterFileManager = masterFileManager;
            _textureManager = textureManager;
        }

        private static IEnumerator CreateTerrainData(float[,] heightMap, Action<TerrainData, float> onReadyCallback)
        {
            Utils.GetExtrema(heightMap, out var minHeight, out var maxHeight);
            yield return null;

            for (var y = 0; y < LandSideLength; y++)
            {
                for (var x = 0; x < LandSideLength; x++)
                {
                    heightMap[y, x] = Utils.ChangeRange(heightMap[y, x], minHeight, maxHeight, 0, 1);
                    yield return null;
                }
            }

            var heightRange = maxHeight - minHeight;
            var maxHeightInMeters = heightRange / Convert.meterInMWUnits;
            const float heightSampleDistance = Convert.ExteriorCellSideLengthInMeters / (LandSideLength - 1);
            yield return null;

            var terrainData = new TerrainData
            {
                heightmapResolution = LandSideLength
            };
            var terrainWidth = (terrainData.heightmapResolution - 1) * heightSampleDistance;
            yield return null;

            if (!Mathf.Approximately(maxHeightInMeters, 0))
            {
                terrainData.size = new Vector3(terrainWidth, maxHeightInMeters, terrainWidth);

                terrainData.SetHeights(0, 0, heightMap);
            }
            else
            {
                terrainData.size = new Vector3(terrainWidth, 1, terrainWidth);
            }

            yield return null;

            onReadyCallback(terrainData, minHeight);
        }

        private static bool IsInQuadrant(int x, int y, int width, int height, Quadrant quadrant)
        {
            return quadrant switch
            {
                Quadrant.BottomLeft => x < width / 2 && y < height / 2,
                Quadrant.TopLeft => x >= width / 2 && y < height / 2,
                Quadrant.BottomRight => x < width / 2 && y >= height / 2,
                Quadrant.TopRight => x >= width / 2 && y >= height / 2,
                _ => false
            };
        }

        private static IEnumerator CreateBaseTerrainLayerAlphaMap(int terrainWidth, int terrainHeight,
            Quadrant quadrant,
            Action<float[,]> onReadyCallback)
        {
            var alphaMap = new float[terrainWidth, terrainHeight];

            for (var y = 0; y < terrainHeight; y++)
            {
                for (var x = 0; x < terrainWidth; x++)
                {
                    alphaMap[x, y] = IsInQuadrant(x, y, terrainWidth, terrainHeight, quadrant) ? 1 : 0;

                    yield return null;
                }
            }

            onReadyCallback(alphaMap);
        }

        private static IEnumerator ConvertAdditionalLayerAlphaMap(float[,] quadrantAlphaMap, Quadrant quadrant,
            int terrainWidth, int terrainHeight, Action<float[,]> onReadyCallback)
        {
            var newAlphaMap = new float[terrainWidth, terrainHeight];
            var quadrantWidth = quadrantAlphaMap.GetLength(0);
            var quadrantHeight = quadrantAlphaMap.GetLength(1);

            var terrainQuadrantWidth = terrainWidth / 2;
            var terrainQuadrantHeight = terrainHeight / 2;

            for (var y = 0; y < terrainHeight; y++)
            {
                for (var x = 0; x < terrainWidth; x++)
                {
                    if (!IsInQuadrant(x, y, terrainWidth, terrainHeight, quadrant)) continue;

                    var qx = (int)((float)(x % terrainQuadrantWidth) / terrainQuadrantWidth * quadrantWidth);
                    var qy = (int)((float)(y % terrainQuadrantHeight) / terrainQuadrantHeight * quadrantHeight);

                    var alpha = BilinearInterpolation(quadrantAlphaMap, qx, qy, quadrantWidth, quadrantHeight);
                    newAlphaMap[x, y] = alpha;

                    yield return null;
                }
            }

            onReadyCallback(newAlphaMap);
        }

        private static float BilinearInterpolation(float[,] map, int x, int y, int width, int height)
        {
            var x1 = Math.Max(0, Math.Min(x, width - 2));
            var x2 = Math.Min(x1 + 1, width - 1);
            var y1 = Math.Max(0, Math.Min(y, height - 2));
            var y2 = Math.Min(y1 + 1, height - 1);

            var qx = x - x1;
            var qy = y - y1;

            var f11 = map[x1, y1];
            var f12 = map[x1, y2];
            var f21 = map[x2, y1];
            var f22 = map[x2, y2];

            var w11 = (1 - qx) * (1 - qy);
            var w21 = qx * (1 - qy);
            var w12 = (1 - qx) * qy;
            var w22 = qx * qy;

            return f11 * w11 + f21 * w21 + f12 * w12 + f22 * w22;
        }

        private static IEnumerator MergeAlphaMaps(Action<float[,]> onReadyCallback, params float[][,] alphaMaps)
        {
            var width = alphaMaps[0].GetLength(0);
            var height = alphaMaps[0].GetLength(1);

            var mergedAlphaMap = new float[width, height];
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var alphaSum = alphaMaps.Sum(alphaMap => alphaMap[x, y]);
                    mergedAlphaMap[x, y] = Mathf.Clamp01(alphaSum);

                    yield return null;
                }
            }

            onReadyCallback(mergedAlphaMap);
        }

        private static IEnumerator MinimizeTerrainLayerCount(List<List<NonLoadedTerrainLayer>> quadrantLayers,
            Action<List<NonLoadedTerrainLayer>> onReadyCallback)
        {
            var alphaMaps = new List<float[,]>();
            var mergedLayers = new List<List<MergedLayer>>();
            foreach (var quadrantLayerList in quadrantLayers)
            {
                var quadrantMergedLayers = new List<MergedLayer>();
                foreach (var layer in quadrantLayerList)
                {
                    var mergedLayer = new MergedLayer(layer.DiffuseMapPath, layer.NormalMapPath, alphaMaps.Count);
                    alphaMaps.Add(layer.AlphaMap);
                    quadrantMergedLayers.Add(mergedLayer);
                }

                mergedLayers.Add(quadrantMergedLayers);
            }

            var quadrantLayerAmount = quadrantLayers.Select(l => l.Count).ToArray();
            var indexLimit = 10;
            var memo = new Dictionary<(int, int, int, int), int>();
            var stack = new Stack<(int, int, int, int, List<MergedLayer>, int)>();
            var bestResultListSize = int.MaxValue;
            List<MergedLayer> bestResultList = null;
            stack.Push((0, 0, 0, 0, new List<MergedLayer>(), 0));

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

                yield return null;

                if (!memo.TryGetValue((i0, i1, i2, i3), out var memoizedListSize))
                {
                    memo[(i0, i1, i2, i3)] = currentSize;
                }
                else
                {
                    if (memoizedListSize <= currentSize) continue;
                    memo[(i0, i1, i2, i3)] = currentSize;
                }

                yield return null;

                var layers = new Dictionary<(string, string), List<int>>();
                if (i0 < quadrantLayerAmount[0])
                {
                    if (layers.ContainsKey((mergedLayers[0][i0].DiffuseMapPath, mergedLayers[0][i0].NormalMapPath)))
                    {
                        layers[(mergedLayers[0][i0].DiffuseMapPath, mergedLayers[0][i0].NormalMapPath)]
                            .AddRange(mergedLayers[0][i0].AlphaMapIndices);
                    }
                    else
                    {
                        layers[(mergedLayers[0][i0].DiffuseMapPath, mergedLayers[0][i0].NormalMapPath)] =
                            new List<int>(mergedLayers[0][i0].AlphaMapIndices);
                    }

                    yield return null;
                }

                if (i1 < quadrantLayerAmount[1])
                {
                    if (layers.ContainsKey((mergedLayers[1][i1].DiffuseMapPath, mergedLayers[1][i1].NormalMapPath)))
                    {
                        layers[(mergedLayers[1][i1].DiffuseMapPath, mergedLayers[1][i1].NormalMapPath)]
                            .AddRange(mergedLayers[1][i1].AlphaMapIndices);
                    }
                    else
                    {
                        layers[(mergedLayers[1][i1].DiffuseMapPath, mergedLayers[1][i1].NormalMapPath)] =
                            new List<int>(mergedLayers[1][i1].AlphaMapIndices);
                    }

                    yield return null;
                }

                if (i2 < quadrantLayerAmount[2])
                {
                    if (layers.ContainsKey((mergedLayers[2][i2].DiffuseMapPath, mergedLayers[2][i2].NormalMapPath)))
                    {
                        layers[(mergedLayers[2][i2].DiffuseMapPath, mergedLayers[2][i2].NormalMapPath)]
                            .AddRange(mergedLayers[2][i2].AlphaMapIndices);
                    }
                    else
                    {
                        layers[(mergedLayers[2][i2].DiffuseMapPath, mergedLayers[2][i2].NormalMapPath)] =
                            new List<int>(mergedLayers[2][i2].AlphaMapIndices);
                    }

                    yield return null;
                }

                if (i3 < quadrantLayerAmount[3])
                {
                    if (layers.ContainsKey((mergedLayers[3][i3].DiffuseMapPath, mergedLayers[3][i3].NormalMapPath)))
                    {
                        layers[(mergedLayers[3][i3].DiffuseMapPath, mergedLayers[3][i3].NormalMapPath)]
                            .AddRange(mergedLayers[3][i3].AlphaMapIndices);
                    }
                    else
                    {
                        layers[(mergedLayers[3][i3].DiffuseMapPath, mergedLayers[3][i3].NormalMapPath)] =
                            new List<int>(mergedLayers[3][i3].AlphaMapIndices);
                    }

                    yield return null;
                }

                var currentMergedLayers = layers.Select(layer =>
                {
                    var (diffuseMap, normalMap) = layer.Key;
                    var alphaMapIndices = layer.Value;
                    var newMergedLayer = new MergedLayer(diffuseMap, normalMap, alphaMapIndices);
                    return newMergedLayer;
                }).ToList();
                
                yield return null;

                var newCurrentList = new List<MergedLayer>(currentList);
                newCurrentList.AddRange(currentMergedLayers);

                for (var advance = 0; advance < 16; advance++)
                {
                    var ni0 = i0 + ((advance & 1) > 0 ? 1 : 0);
                    var ni1 = i1 + ((advance & 2) > 0 ? 1 : 0);
                    var ni2 = i2 + ((advance & 4) > 0 ? 1 : 0);
                    var ni3 = i3 + ((advance & 8) > 0 ? 1 : 0);
                    stack.Push((ni0, ni1, ni2, ni3, newCurrentList, newCurrentList.Count));
                    
                    yield return null;
                }
            }

            var resultingMergedLayers = bestResultList;

            List<NonLoadedTerrainLayer> result = new();
            foreach (var mergedLayer in resultingMergedLayers)
            {
                var layerAlphaMaps = mergedLayer.AlphaMapIndices.Select(index => alphaMaps[index]).ToArray();
                float[,] mergedAlphaMap = null;
                var mergedAlphaMapCoroutine =
                    MergeAlphaMaps(alphaMap => { mergedAlphaMap = alphaMap; }, layerAlphaMaps);
                while (mergedAlphaMapCoroutine.MoveNext())
                    yield return null;

                result.Add(new NonLoadedTerrainLayer(mergedLayer.DiffuseMapPath, mergedLayer.NormalMapPath,
                    mergedAlphaMap));
                yield return null;
            }

            onReadyCallback(result);
        }

        private static IEnumerator ConvertAlphaMapListTo3DArray(List<float[,]> alphaMaps,
            Action<float[,,]> onReadyCallback)
        {
            var width = alphaMaps[0].GetLength(0);
            var height = alphaMaps[0].GetLength(1);
            var layers = alphaMaps.Count;

            var alphaMap = new float[width, height, layers];
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    for (var layer = 0; layer < layers; layer++)
                    {
                        alphaMap[x, y, layer] = alphaMaps[layer][x, y];

                        yield return null;
                    }
                }
            }

            onReadyCallback(alphaMap);
        }

        private IEnumerator GetTexturePaths(uint landTextureFormID, Action<string, string> onReadyCallback)
        {
            if (landTextureFormID == 0) yield break;
            var landTextureRecordTask = _masterFileManager.GetFromFormIDTask(landTextureFormID);
            while (!landTextureRecordTask.IsCompleted)
                yield return null;

            var landTextureRecord = landTextureRecordTask.Result;
            if (landTextureRecord is not LTEX { TextureFormID: not null } ltex) yield break;
            var textureRecordTask = _masterFileManager.GetFromFormIDTask(ltex.TextureFormID.Value);
            while (!textureRecordTask.IsCompleted)
                yield return null;

            var textureRecord = textureRecordTask.Result;

            if (textureRecord is not TXST texture) yield break;

            var diffuseMapPath = texture.DiffuseMapPath;
            var normalMapPath = texture.NormalMapPath;

            if (diffuseMapPath == null)
            {
                diffuseMapPath = DefaultDiffuseTexturePath;
                normalMapPath = DefaultNormalMapPath;
            }

            if (!diffuseMapPath.StartsWith(TexturePathPrefix, ignoreCase: true, CultureInfo.InvariantCulture))
                diffuseMapPath = $"{TexturePathPrefix}\\{diffuseMapPath}";
            diffuseMapPath = diffuseMapPath.Replace('/', '\\');

            if (normalMapPath != null &&
                !normalMapPath.StartsWith(TexturePathPrefix, ignoreCase: true, CultureInfo.InvariantCulture))
                normalMapPath = $"{TexturePathPrefix}\\{normalMapPath}";
            normalMapPath = normalMapPath?.Replace('/', '\\');

            onReadyCallback(diffuseMapPath, normalMapPath);
        }

        private IEnumerator GetDiffuseAndNormalMapByPaths(string diffuseMapPath, string normalMapPath,
            Action<Texture2D, Texture2D> onReadyCallback)
        {
            Texture2D diffuseTexture = null;
            if (!string.IsNullOrEmpty(diffuseMapPath))
            {
                var diffuseTextureCoroutine = _textureManager.GetMap<Texture2D>(TextureType.DIFFUSE, diffuseMapPath,
                    texture2D => { diffuseTexture = texture2D; });
                if (diffuseTextureCoroutine != null)
                    while (diffuseTextureCoroutine.MoveNext())
                        yield return null;
            }

            Texture2D normalMap = null;
            if (!string.IsNullOrEmpty(normalMapPath))
            {
                var normalMapCoroutine = _textureManager.GetMap<Texture2D>(TextureType.NORMAL, normalMapPath,
                    texture2D => { normalMap = texture2D; });
                if (normalMapCoroutine != null)
                    while (normalMapCoroutine.MoveNext())
                        yield return null;
            }

            onReadyCallback(diffuseTexture, normalMap);
        }

        private IEnumerator GetBaseTerrainLayerInfo(LAND record, int terrainWidth, int terrainHeight,
            Action<Dictionary<Quadrant, List<NonLoadedTerrainLayer>>> onReadyCallback)
        {
            var baseTextures = new Dictionary<Quadrant, List<NonLoadedTerrainLayer>>();
            var missingQuadrants = new List<Quadrant>
                { Quadrant.BottomLeft, Quadrant.BottomRight, Quadrant.TopLeft, Quadrant.TopRight };
            foreach (var baseTexture in record.BaseTextures)
            {
                string diffuseMapPath = null;
                string normalMapPath = null;

                var getTexturePathsCoroutine = GetTexturePaths(baseTexture.LandTextureFormID, (diffuse, normal) =>
                {
                    diffuseMapPath = diffuse;
                    normalMapPath = normal;
                });
                while (getTexturePathsCoroutine.MoveNext())
                    yield return null;

                float[,] alphaMap = null;
                var createAlphaMapCoroutine = CreateBaseTerrainLayerAlphaMap(terrainWidth, terrainHeight,
                    (Quadrant)baseTexture.Quadrant, createdAlphaMap => { alphaMap = createdAlphaMap; });
                while (createAlphaMapCoroutine.MoveNext())
                    yield return null;

                baseTextures[(Quadrant)baseTexture.Quadrant] = new List<NonLoadedTerrainLayer>
                {
                    new(diffuseMapPath, normalMapPath, alphaMap)
                };
                missingQuadrants.Remove((Quadrant)baseTexture.Quadrant);
            }

            foreach (var missingQuadrant in missingQuadrants)
            {
                float[,] alphaMap = null;
                var createAlphaMapCoroutine = CreateBaseTerrainLayerAlphaMap(terrainWidth, terrainHeight,
                    missingQuadrant, createdAlphaMap => { alphaMap = createdAlphaMap; });
                while (createAlphaMapCoroutine.MoveNext())
                    yield return null;

                baseTextures[missingQuadrant] = new List<NonLoadedTerrainLayer>
                {
                    new(DefaultDiffuseTexturePath, DefaultNormalMapPath, alphaMap)
                };
            }

            onReadyCallback(baseTextures);
        }

        private IEnumerator GetAdditionalTerrainLayersInfo(LAND record, int terrainWidth, int terrainHeight,
            Action<Dictionary<Quadrant, List<NonLoadedTerrainLayer>>> onReadyCallback)
        {
            var additionalTextures = new Dictionary<Quadrant, List<NonLoadedTerrainLayer>>();
            record.AdditionalTextures.Sort((a, b) => a.Layer.CompareTo(b.Layer));
            foreach (var additionalTexture in record.AdditionalTextures)
            {
                string diffuseMapPath = null;
                string normalMapPath = null;

                var getTexturePathsCoroutine = GetTexturePaths(additionalTexture.LandTextureFormID, (diffuse, normal) =>
                {
                    diffuseMapPath = diffuse;
                    normalMapPath = normal;
                });
                while (getTexturePathsCoroutine.MoveNext())
                    yield return null;

                float[,] convertedAlphaMap = null;
                var convertedAlphaMapCoroutine = ConvertAdditionalLayerAlphaMap(
                    additionalTexture.QuadrantAlphaMap,
                    (Quadrant)additionalTexture.Quadrant, terrainWidth, terrainHeight,
                    alphaMap => { convertedAlphaMap = alphaMap; });
                while (convertedAlphaMapCoroutine.MoveNext())
                    yield return null;

                if (additionalTextures.TryGetValue((Quadrant)additionalTexture.Quadrant, out var quadrantLayers))
                {
                    quadrantLayers.Add(new NonLoadedTerrainLayer(diffuseMapPath, normalMapPath, convertedAlphaMap));
                }
                else
                {
                    additionalTextures[(Quadrant)additionalTexture.Quadrant] = new List<NonLoadedTerrainLayer>
                    {
                        new(diffuseMapPath, normalMapPath, convertedAlphaMap)
                    };
                }

                yield return null;
            }

            onReadyCallback(additionalTextures);
        }

        private IEnumerator LoadTerrainLayers(List<NonLoadedTerrainLayer> terrainLayers,
            Action<List<TerrainTexture>> onReadyCallback)
        {
            var loadedTerrainLayers = new List<TerrainTexture>();
            foreach (var terrainLayer in terrainLayers)
            {
                Texture2D diffuseTexture = null;
                Texture2D normalMap = null;

                var getDiffuseAndNormalMapCoroutine = GetDiffuseAndNormalMapByPaths(terrainLayer.DiffuseMapPath,
                    terrainLayer.NormalMapPath, (diffuse, normal) =>
                    {
                        diffuseTexture = diffuse;
                        normalMap = normal;
                    });
                while (getDiffuseAndNormalMapCoroutine.MoveNext())
                    yield return null;

                loadedTerrainLayers.Add(new TerrainTexture(diffuseTexture, normalMap, terrainLayer.AlphaMap));
                yield return null;
            }

            onReadyCallback(loadedTerrainLayers);
        }

        private static IEnumerator PaintTextures(TerrainData terrainData, List<TerrainTexture> textures)
        {
            var terrainLayers = new List<TerrainLayer>();
            var alphaMaps = new List<float[,]>();
            foreach (var texture in textures)
            {
                var terrainLayer = new TerrainLayer
                {
                    diffuseTexture = texture.DiffuseTexture,
                    normalMapTexture = texture.NormalMap,
                    tileOffset = Vector2.zero,
                    tileSize = Vector2.one * 2
                };
                terrainLayers.Add(terrainLayer);
                alphaMaps.Add(texture.AlphaMap);
                yield return null;
            }

            terrainData.terrainLayers = terrainLayers.ToArray();
            yield return null;

            float[,,] newAlphaMaps = null;
            var convertCoroutine = ConvertAlphaMapListTo3DArray(alphaMaps,
                convertedAlphaMaps => { newAlphaMaps = convertedAlphaMaps; });
            while (convertCoroutine.MoveNext())
                yield return null;

            terrainData.SetAlphamaps(0, 0, newAlphaMaps);
        }

        private IEnumerator CreateTerrain(CELL cell, LAND land, Action<GameObject> onReadyCallback)
        {
            TerrainData terrainData = null;
            float minHeight = 0;
            var terrainDataCoroutine = CreateTerrainData(land.VertexHeightMap, (data, calculatedMinHeight) =>
            {
                terrainData = data;
                minHeight = calculatedMinHeight;
            });
            while (terrainDataCoroutine.MoveNext())
                yield return null;

            Dictionary<Quadrant, List<NonLoadedTerrainLayer>> terrainLayers = new();

            var getBaseLayerInfoCoroutine =
                GetBaseTerrainLayerInfo(land, terrainData.alphamapWidth, terrainData.alphamapHeight,
                    baseTerrainLayers => { terrainLayers = baseTerrainLayers; });
            while (getBaseLayerInfoCoroutine.MoveNext())
                yield return null;

            var getAdditionalLayersInfoCoroutine = GetAdditionalTerrainLayersInfo(land, terrainData.alphamapWidth,
                terrainData.alphamapHeight,
                additionalTerrainLayers =>
                {
                    Quadrant[] quadrants =
                    {
                        Quadrant.BottomLeft, Quadrant.BottomRight, Quadrant.TopLeft, Quadrant.TopRight
                    };
                    foreach (var quadrant in quadrants)
                    {
                        if (!additionalTerrainLayers.TryGetValue(quadrant, out var additionalLayers))
                            continue;
                        if (terrainLayers.TryGetValue(quadrant, out var layerList))
                        {
                            layerList.AddRange(additionalLayers);
                        }
                        else
                        {
                            terrainLayers[quadrant] = additionalLayers;
                        }
                    }
                });
            while (getAdditionalLayersInfoCoroutine.MoveNext())
                yield return null;

            List<NonLoadedTerrainLayer> mergedLayers = null;
            var minimizeLayerCountCoroutine = MinimizeTerrainLayerCount(terrainLayers.Values.ToList(),
                minimizedLayers => { mergedLayers = minimizedLayers; });
            while (minimizeLayerCountCoroutine.MoveNext())
                yield return null;

            List<TerrainTexture> loadedTerrainLayers = null;
            var loadTerrainLayersCoroutine = LoadTerrainLayers(mergedLayers,
                terrainTextures => { loadedTerrainLayers = terrainTextures; });
            while (loadTerrainLayersCoroutine.MoveNext())
                yield return null;

            var paintCoroutine = PaintTextures(terrainData, loadedTerrainLayers);
            while (paintCoroutine.MoveNext())
                yield return null;

            var gameObject = new GameObject("terrain");

            var terrain = gameObject.AddComponent<Terrain>();
            terrain.terrainData = terrainData;
            terrain.materialTemplate = new Material(Shader.Find(DefaultTerrainShader));
            yield return null;

            gameObject.AddComponent<TerrainCollider>().terrainData = terrainData;

            var terrainPosition = new Vector3(Convert.ExteriorCellSideLengthInMeters * cell.XGridPosition,
                minHeight / Convert.meterInMWUnits, Convert.ExteriorCellSideLengthInMeters * cell.YGridPosition);

            gameObject.transform.position = terrainPosition;
            yield return null;

            onReadyCallback(gameObject);
        }

        protected override IEnumerator PreprocessRecord(CELL cell, LAND record, GameObject parent, LoadCause loadCause)
        {
            return CreateTerrain(cell, record, terrain => { terrain.transform.parent = parent.transform; });
        }
    }
}