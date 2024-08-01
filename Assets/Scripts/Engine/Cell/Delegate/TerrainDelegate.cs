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

    public struct BaseTexture
    {
        public readonly Quadrant Quadrant;
        public readonly Texture2D DiffuseTexture;
        [CanBeNull] public readonly Texture2D NormalMap;

        public BaseTexture(Quadrant quadrant, Texture2D diffuseTexture, [CanBeNull] Texture2D normalMap)
        {
            Quadrant = quadrant;
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

        private readonly MasterFileManager _masterFileManager;
        private readonly TextureManager _textureManager;

        public TerrainDelegate(MasterFileManager masterFileManager, TextureManager textureManager)
        {
            _masterFileManager = masterFileManager;
            _textureManager = textureManager;
        }

        private static TerrainData CreateTerrainData(float[,] heightMap, out float minHeight)
        {
            Utils.GetExtrema(heightMap, out minHeight, out var maxHeight);

            for (var y = 0; y < LandSideLength; y++)
            {
                for (var x = 0; x < LandSideLength; x++)
                {
                    heightMap[y, x] = Utils.ChangeRange(heightMap[y, x], minHeight, maxHeight, 0, 1);
                }
            }

            var heightRange = maxHeight - minHeight;
            var maxHeightInMeters = heightRange / Convert.meterInMWUnits;
            const float heightSampleDistance = Convert.ExteriorCellSideLengthInMeters / (LandSideLength - 1);

            var terrainData = new TerrainData
            {
                heightmapResolution = LandSideLength
            };
            var terrainWidth = (terrainData.heightmapResolution - 1) * heightSampleDistance;

            if (!Mathf.Approximately(maxHeightInMeters, 0))
            {
                terrainData.size = new Vector3(terrainWidth, maxHeightInMeters, terrainWidth);

                terrainData.SetHeights(0, 0, heightMap);
            }
            else
            {
                terrainData.size = new Vector3(terrainWidth, 1, terrainWidth);
            }

            return terrainData;
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
        
        private IEnumerator LoadTerrainBaseTextures(LAND record, Action<List<BaseTexture>> onReadyCallback)
        {
            var baseTextures = new List<BaseTexture>();
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

                var loadedBaseTexture = new BaseTexture((Quadrant)baseTexture.Quadrant, diffuseTexture, normalMap);
                baseTextures.Add(loadedBaseTexture);
            }

            onReadyCallback(baseTextures);
        }

        private static bool IsInQuadrant(int x, int y, int width, int height, Quadrant quadrant)
        {
            return quadrant switch
            {
                Quadrant.BottomLeft => x < width / 2 && y < height / 2,
                Quadrant.BottomRight => x >= width / 2 && y < height / 2,
                Quadrant.TopLeft => x < width / 2 && y >= height / 2,
                Quadrant.TopRight => x >= width / 2 && y >= height / 2,
                _ => false
            };
        }

        private static float[,] CreateBaseTextureSplatMap(TerrainData terrainData, Quadrant quadrant)
        {
            var width = terrainData.alphamapWidth;
            var height = terrainData.alphamapHeight;
            
            var splatMap = new float[width, height];
            
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    splatMap[x, y] = IsInQuadrant(x, y, width, height, quadrant) ? 1 : 0;
                }
            }
            
            return splatMap;
        }
        
        private static float[,,] ConvertSplatMapListTo3DArray(List<float[,]> splatMaps)
        {
            var width = splatMaps[0].GetLength(0);
            var height = splatMaps[0].GetLength(1);
            var layers = splatMaps.Count;

            var splatMap = new float[width, height, layers];
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    for (var layer = 0; layer < layers; layer++)
                    {
                        splatMap[x, y, layer] = splatMaps[layer][x, y];
                    }
                }
            }

            return splatMap;
        }

        private static void PaintBaseTextures(TerrainData terrainData, List<BaseTexture> baseTextures)
        {
            var terrainLayers = new List<TerrainLayer>();
            var splatMaps = new List<float[,]>();
            foreach (var baseTexture in baseTextures)
            {
                var terrainLayer = new TerrainLayer
                {
                    diffuseTexture = baseTexture.DiffuseTexture,
                    normalMapTexture = baseTexture.NormalMap,
                    tileOffset = Vector2.zero,
                    tileSize = Vector2.one
                };
                terrainLayers.Add(terrainLayer);
                splatMaps.Add(CreateBaseTextureSplatMap(terrainData, baseTexture.Quadrant));
            }

            terrainData.terrainLayers = terrainLayers.ToArray();
            terrainData.SetAlphamaps(0, 0, ConvertSplatMapListTo3DArray(splatMaps));
        }

        private IEnumerator CreateTerrain(CELL cell, LAND land, Action<GameObject> onReadyCallback)
        {
            var terrainData = CreateTerrainData(land.VertexHeightMap, out var minHeight);
            yield return null;

            var loadBaseTexturesCoroutine =
                LoadTerrainBaseTextures(land, baseTextures => { PaintBaseTextures(terrainData, baseTextures); });
            while (loadBaseTexturesCoroutine.MoveNext())
                yield return null;

            var gameObject = new GameObject("terrain");

            var terrain = gameObject.AddComponent<Terrain>();
            terrain.terrainData = terrainData;
            terrain.materialTemplate = new Material(Shader.Find("Nature/Terrain/Standard"));
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