using System.Collections.Generic;
using System;
using UnityEngine;

public static class TileHelpers
{
    public static Vector2Int[] GetAdjacentTiles(Vector2Int tile)
    {
        if (tile == null)
        {
            throw new NullReferenceException();
        }
        return new Vector2Int[]
        {
            new(tile.x - 1, tile.y),
            new(tile.x + 1, tile.y),
            new(tile.x, tile.y - 1),
            new(tile.x, tile.y + 1),
            new(Mathf.Abs(tile.y) % 2 == 1 ? tile.x + 1 : tile.x - 1, tile.y - 1),
            new(Mathf.Abs(tile.y) % 2 == 0 ? tile.x - 1 : tile.x + 1, tile.y + 1)
        };
    }

    public static int GetTileDistance(Vector2Int tileA, Vector2Int tileB)
    {
        List<Vector2Int> alreadyChecked = new();
        List<Vector2Int> checking = new() { tileA };
        List<Vector2Int> nextChecks = new();
        int result = 0;
        while(result < 99999)
        {
            while(checking.Count > 0)
            {
                if (checking[0].x == tileB.x && checking[0].y == tileB.y)
                {
                    return result;
                }
                else
                {
                    foreach(Vector2Int candidate in GetAdjacentTiles(checking[0]))
                    {
                        if (!(alreadyChecked.Contains(candidate) || checking.Contains(candidate) || nextChecks.Contains(candidate)))
                        {
                            nextChecks.Add(candidate);
                        }
                    }
                    alreadyChecked.Add(checking[0]);
                    checking.RemoveAt(0);
                }
            }
            checking = nextChecks;
            nextChecks = new();
            result++;
        }
        return -1;
    }

    public static List<Vector2Int> GetAllTilesWithinRange(Vector2Int tile, uint range)
    {
        List<Vector2Int> previousLayers = new() { tile }, currentLayer = new(), nextLayer = new();
        foreach(Vector2Int currentLayerTile in GetAdjacentTiles(tile))
        {
            currentLayer.Add(currentLayerTile);
        }
        for(int layer = 0; layer < range; layer++)
        {
            while(currentLayer.Count > 0)
            {
                foreach (Vector2Int candidate in GetAdjacentTiles(currentLayer[0]))
                {
                    if (!(previousLayers.Contains(candidate) || currentLayer.Contains(candidate) || nextLayer.Contains(candidate)))
                    {
                        nextLayer.Add(candidate);
                    }
                }
                previousLayers.Add(currentLayer[0]);
                currentLayer.RemoveAt(0);
            }
            currentLayer = nextLayer;
            nextLayer = new();
        }
        return previousLayers;
    }
}
