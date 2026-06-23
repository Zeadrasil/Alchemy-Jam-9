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
            new(tile.y % 2 == 1 ? tile.x + 1 : tile.x - 1, tile.y - 1),
            new(tile.y % 2 == 0 ? tile.x - 1 : tile.x + 1, tile.y + 1)
        };
    }
}
