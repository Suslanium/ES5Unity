using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
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
            onReadyCallback(texture.DiffuseMapPath, texture.NormalMapPath);
        }

        private IEnumerator GetDiffuseAndNormalMapByPaths(string diffuseMapPath, string normalMapPath,
            Action<Texture2D, Texture2D> onReadyCallback)
        {
            if (!diffuseMapPath.StartsWith(TexturePathPrefix, ignoreCase: true, CultureInfo.InvariantCulture))
                diffuseMapPath = $"{TexturePathPrefix}\\{diffuseMapPath}";

            if (!normalMapPath.StartsWith(TexturePathPrefix, ignoreCase: true, CultureInfo.InvariantCulture))
                normalMapPath = $"{TexturePathPrefix}\\{normalMapPath}";

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

        private IEnumerator LoadTerrainBaseTextures(LAND record, int terrainWidth, int terrainHeight,
            Action<List<TerrainTexture>> onReadyCallback)
        {
            var baseTextures = new List<TerrainTexture>();
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

                if (diffuseMapPath == null)
                {
                    diffuseMapPath = DefaultDiffuseTexturePath;
                    normalMapPath = DefaultNormalMapPath;
                }

                Texture2D diffuseMap = null;
                Texture2D normalMap = null;
                var getDiffuseAndNormalMapCoroutine = GetDiffuseAndNormalMapByPaths(diffuseMapPath, normalMapPath,
                    (diffuse, normal) =>
                    {
                        diffuseMap = diffuse;
                        normalMap = normal;
                    });
                while (getDiffuseAndNormalMapCoroutine.MoveNext())
                    yield return null;

                float[,] alphaMap = null;
                var createAlphaMapCoroutine = CreateBaseTextureAlphaMap(terrainWidth, terrainHeight,
                    (Quadrant)baseTexture.Quadrant, createdAlphaMap => { alphaMap = createdAlphaMap; });
                while (createAlphaMapCoroutine.MoveNext())
                    yield return null;

                baseTextures.Add(new TerrainTexture(diffuseMap, normalMap, alphaMap));
                missingQuadrants.Remove((Quadrant)baseTexture.Quadrant);
            }

            foreach (var missingQuadrant in missingQuadrants)
            {
                Texture2D diffuseMap = null;
                Texture2D normalMap = null;
                var getDiffuseAndNormalMapCoroutine = GetDiffuseAndNormalMapByPaths(DefaultDiffuseTexturePath,
                    DefaultNormalMapPath,
                    (diffuse, normal) =>
                    {
                        diffuseMap = diffuse;
                        normalMap = normal;
                    });
                while (getDiffuseAndNormalMapCoroutine.MoveNext())
                    yield return null;

                float[,] alphaMap = null;
                var createAlphaMapCoroutine = CreateBaseTextureAlphaMap(terrainWidth, terrainHeight,
                    missingQuadrant, createdAlphaMap => { alphaMap = createdAlphaMap; });
                while (createAlphaMapCoroutine.MoveNext())
                    yield return null;

                baseTextures.Add(new TerrainTexture(diffuseMap, normalMap, alphaMap));
            }

            onReadyCallback(baseTextures);
        }

        private IEnumerator LoadTerrainAdditionalTextures(LAND record, int terrainWidth, int terrainHeight,
            Action<List<TerrainTexture>> onReadyCallback)
        {
            var additionalTextures = new List<TerrainTexture>();
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

                if (diffuseMapPath == null)
                {
                    diffuseMapPath = DefaultDiffuseTexturePath;
                    normalMapPath = DefaultNormalMapPath;
                }

                Texture2D diffuseMap = null;
                Texture2D normalMap = null;
                var getDiffuseAndNormalMapCoroutine = GetDiffuseAndNormalMapByPaths(diffuseMapPath, normalMapPath,
                    (diffuse, normal) =>
                    {
                        diffuseMap = diffuse;
                        normalMap = normal;
                    });
                while (getDiffuseAndNormalMapCoroutine.MoveNext())
                    yield return null;

                float[,] convertedAlphaMap = null;
                var convertedAlphaMapCoroutine = ConvertAdditionalTextureAlphaMap(
                    additionalTexture.QuadrantAlphaMap,
                    (Quadrant)additionalTexture.Quadrant, terrainWidth, terrainHeight,
                    (alphaMap) => { convertedAlphaMap = alphaMap; });
                while (convertedAlphaMapCoroutine.MoveNext())
                    yield return null;

                var loadedAdditionalTexture = new TerrainTexture(diffuseMap, normalMap, convertedAlphaMap);
                additionalTextures.Add(loadedAdditionalTexture);
            }

            onReadyCallback(additionalTextures);
        }

        private static IEnumerator ConvertAdditionalTextureAlphaMap(float[,] quadrantAlphaMap, Quadrant quadrant,
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

        private static IEnumerator CreateBaseTextureAlphaMap(int terrainWidth, int terrainHeight, Quadrant quadrant,
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

            List<TerrainTexture> textures = new();

            var loadBaseTexturesCoroutine =
                LoadTerrainBaseTextures(land, terrainData.alphamapWidth, terrainData.alphamapHeight,
                    loadedBaseTextures => { textures.AddRange(loadedBaseTextures); });
            while (loadBaseTexturesCoroutine.MoveNext())
                yield return null;

            var loadAdditionalTexturesCoroutine = LoadTerrainAdditionalTextures(land, terrainData.alphamapWidth,
                terrainData.alphamapHeight,
                loadedAdditionalTextures => { textures.AddRange(loadedAdditionalTextures); });
            while (loadAdditionalTexturesCoroutine.MoveNext())
                yield return null;

            var paintCoroutine = PaintTextures(terrainData, textures);
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