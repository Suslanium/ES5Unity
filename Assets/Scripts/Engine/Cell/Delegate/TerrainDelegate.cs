using System.Collections;
using Engine.Cell.Delegate.Interfaces;
using Engine.Core;
using MasterFile.MasterFileContents.Records;
using UnityEngine;

namespace Engine.Cell.Delegate
{
    public class TerrainDelegate : CellRecordPreprocessDelegate<LAND>
    {
        private const int LandSideLength = Convert.ExteriorCellSideLengthInSamples;
        
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

        private static GameObject CreateTerrain(CELL cell, LAND land)
        {
            var terrainData = CreateTerrainData(land.VertexHeightMap, out var minHeight);

            var gameObject = new GameObject("terrain");

            var terrain = gameObject.AddComponent<Terrain>();
            terrain.terrainData = terrainData;

            gameObject.AddComponent<TerrainCollider>().terrainData = terrainData;

            var terrainPosition = new Vector3(Convert.ExteriorCellSideLengthInMeters * cell.XGridPosition,
                minHeight / Convert.meterInMWUnits, Convert.ExteriorCellSideLengthInMeters * cell.YGridPosition);

            gameObject.transform.position = terrainPosition;
            
            return gameObject;
        }

        protected override IEnumerator PreprocessRecord(CELL cell, LAND record, GameObject parent, LoadCause loadCause)
        {
            var terrain = CreateTerrain(cell, record);
            terrain.transform.parent = parent.transform;

            yield break;
        }
    }
}