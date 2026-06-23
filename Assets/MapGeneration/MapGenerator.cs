using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MapGenerator : MonoBehaviour
{
    [SerializeField] private bool generateNew = true;
    [SerializeField] private Tilemap fullMap;
    [SerializeField] private TileBase navigableTile;
    [SerializeField] private TileBase blockingTile;
    [SerializeField] private TileBase trappedTile;
    [SerializeField] private TileBase entranceTile;
    [SerializeField] private TileBase exitTile;
    [SerializeField] private TileBase lootTile;
    [SerializeField] private GameObject[] trapPrefabs;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //Allow for loading between
        if(generateNew)
        {
            //TODO: connect gamemanager to vary map size
            int maxTiles = 40;
            List<Vector2Int> availableTiles = new()
            {
                Vector2Int.zero
            };
            List<Vector2Int> placedTiles = new();
            //Select navigable tiles
            for(int currentTile = 0; currentTile < maxTiles; currentTile++)
            {
                int tileIndex = Random.Range(0, availableTiles.Count);
                Vector2Int tileCoords = availableTiles[tileIndex];
                availableTiles.RemoveAt(tileIndex);
                placedTiles.Add(tileCoords);
                Vector2Int[] potentialTiles = TileHelpers.GetAdjacentTiles(tileCoords);
                foreach (Vector2Int potentialTile in potentialTiles)
                {
                    if(!(availableTiles.Contains(potentialTile) || placedTiles.Contains(potentialTile)))
                    {
                        availableTiles.Add(potentialTile);
                    }
                }
            }
            //Generate navigable tiles
            foreach(Vector2Int tile in placedTiles)
            {
                fullMap.SetTile(new Vector3Int(tile.x, tile.y), navigableTile);
            }
            List<Vector2Int> occupiedTiles = new();
            //Entrance/Exit first as they have most restrictive placing requirements
            Vector2Int exitCoords = Vector2Int.zero, entranceCoords = Vector2Int.zero;
            //Should be mathematically guaranteed to have a possible pair given worst case generation, adding attempt check anyways
            float mintravelDistance = Mathf.Sqrt(maxTiles) - 1;
            for(int remainingPlacementAttempts = 1000; Vector2Int.Distance(entranceCoords, exitCoords) < mintravelDistance && remainingPlacementAttempts > 0; remainingPlacementAttempts--)
            {
                int entranceIndex = Random.Range(0, placedTiles.Count);
                entranceCoords = placedTiles[entranceIndex];
                //Prevent entrance/exit from generating on same spot on final attempt
                placedTiles.RemoveAt(entranceIndex);
                exitCoords = placedTiles[Random.Range(0, placedTiles.Count)];
                //Restore entrance as a valid tile
                placedTiles.Add(entranceCoords);
            }
            occupiedTiles.Add(entranceCoords);
            occupiedTiles.Add(exitCoords);
            //TODO: connect map size to vary trap density, add randomness
            Dictionary<Vector2Int, string> placedTraps = new();
            int traps = 1;
            for (int currentTrap = 0; currentTrap < traps; currentTrap++)
            {
                Vector2Int coords = placedTiles[Random.Range(0, placedTiles.Count)];
                //Prevent from generating on occupied tiles
                while (occupiedTiles.Contains(coords))
                {
                    coords = placedTiles[Random.Range(0, placedTiles.Count)];
                }
                //TODO: add trap variation
                Instantiate(trapPrefabs[0], fullMap.CellToWorld(new Vector3Int(coords.x, coords.y)), Quaternion.identity);
                occupiedTiles.Add(coords);
                placedTraps.Add(coords, "BearTrap");
            }

            //TODO: connect loot generation
            List<Vector2Int> placedLoot = new();
            int loots = 2;
            for(int currentLoot = 0; currentLoot < loots; currentLoot++)
            {
                Vector2Int coords = placedTiles[Random.Range(0, placedTiles.Count)];
                //Prevent from generating on occupied tiles
                while (occupiedTiles.Contains(coords))
                {
                    coords = placedTiles[Random.Range(0, placedTiles.Count)];
                }
                occupiedTiles.Add(coords);
                placedLoot.Add(coords);
            }

            foreach (Vector2Int coord in placedLoot)
            {
                fullMap.SetTile(new Vector3Int(coord.x, coord.y), lootTile);
            }
            foreach(Vector2Int coord in placedTraps.Keys)
            {
                fullMap.SetTile(new Vector3Int(coord.x, coord.y), trappedTile);
            }
            fullMap.SetTile(new Vector3Int(entranceCoords.x, entranceCoords.y), entranceTile);
            fullMap.SetTile(new Vector3Int(exitCoords.x, exitCoords.y), exitTile);
            //TODO: Connect placed tiles to manager to allow behavior
            foreach(Vector2Int coord in availableTiles)
            {
                fullMap.SetTile(new Vector3Int(coord.x, coord.y), blockingTile);
            }
        }
        else
        {
            //TODO: Implement map loading to allow for return from combat/closing game
        }
    }

    
}
