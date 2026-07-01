using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public class MapGenerator : MonoBehaviour
{
    [SerializeField] private Tilemap fullMap;
    [SerializeField] private TileBase navigableTile;
    [SerializeField] private TileBase blockingTile;
    [SerializeField] private TileBase trappedTile;
    [SerializeField] private TileBase entranceTile;
    [SerializeField] private TileBase exitTile;
    [SerializeField] private GameObject[] trapPrefabs;
    [SerializeField] private GameObject chestPrefab;
    [SerializeField] private List<string> enemyNames = new();
    [SerializeField] private List<GameObject> enemyPrefabs = new();
    [SerializeField] private InputActionAsset inputActionAsset;
    [SerializeField] private Tilemap overlayMap;
    [SerializeField] private TileBase availableTile;
    [SerializeField] private TileBase selectedTile;
    [SerializeField] private GameObject player;
    private List<GameObject> currentEnemies = new();

    private int stage = 0;
    private Vector2Int previousMouseCoords = new();
    private List<Vector2Int> validPlayerMovementLocations = new();
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        AudioManager.Instance.PlayMain();
        //Allow for loading between
        if(!ExplorationManager.Instance.GetFloorGenerated())
        {
            ExplorationManager.Instance.ResetFloor();

            int maxTiles = 30 + 10 * ExplorationManager.Instance.GetFloor();
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
            int traps = ExplorationManager.Instance.GetFloor();
            for (int currentTrap = 0; currentTrap < traps; currentTrap++)
            {
                Vector2Int coords = placedTiles[Random.Range(0, placedTiles.Count)];
                //Prevent from generating on occupied tiles
                while (occupiedTiles.Contains(coords))
                {
                    coords = placedTiles[Random.Range(0, placedTiles.Count)];
                }
                //TODO: add trap variation
                ExplorationManager.Instance.RegisterTrap(coords, Instantiate(trapPrefabs[0], fullMap.CellToWorld(new Vector3Int(coords.x, coords.y)), Quaternion.identity).GetComponent<Trap>());
                occupiedTiles.Add(coords);
                placedTraps.Add(coords, "BearTrap");
            }
            ExplorationManager.Instance.RegisterNavigable(placedTiles);
            //TODO: connect loot generation
            List<Vector2Int> placedLoot = new();
            int loots = 1 + ExplorationManager.Instance.GetFloor() / 5;
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
                ExplorationManager.Instance.RegisterLoot(coord, Instantiate(chestPrefab, fullMap.CellToWorld(new(coord.x, coord.y)), Quaternion.identity));
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

            for(int currentEnemy = 0, maxEnemies = 3 + ExplorationManager.Instance.GetFloor(); currentEnemy < maxEnemies;  currentEnemy++)
            {
                Vector2Int location = placedTiles[Random.Range(0, placedTiles.Count)];
                if(occupiedTiles.Contains(location))
                {
                    currentEnemy--;
                }
                else
                {
                    int enemyType = Random.Range(0, enemyNames.Count);
                    currentEnemies.Add(Instantiate(enemyPrefabs[enemyType], fullMap.CellToWorld(new(location.x, location.y)), Quaternion.identity));
                    ExplorationManager.Instance.RegisterEnemy(location, enemyNames[enemyType]);
                    occupiedTiles.Add(location);
                }
            }
            player.transform.position = fullMap.CellToWorld(new(entranceCoords.x, entranceCoords.y));
            ExplorationManager.Instance.RegisterEntrance(entranceCoords);
            ExplorationManager.Instance.RegisterExit(exitCoords);
            ExplorationManager.Instance.SetFloorGenerated();
        }
        else
        {
            List<Vector2Int> navigableTiles = ExplorationManager.Instance.GetNavigableTiles();
            foreach(Vector2Int coord in navigableTiles)
            {
                fullMap.SetTile(new(coord.x, coord.y), navigableTile);
            }
            //TODO: Add trap variation
            List<Vector2Int> trapTiles = ExplorationManager.Instance.GetTrapLocations();
            foreach(Vector2Int coords in trapTiles)
            {
                Instantiate(trapPrefabs[0], fullMap.CellToWorld(new Vector3Int(coords.x, coords.y)), Quaternion.identity);
                fullMap.SetTile(new(coords.x, coords.y), trappedTile);
            }

            List<Vector2Int> loots = ExplorationManager.Instance.GetLootLocations();
            foreach(Vector2Int coord in loots)
            {
                ExplorationManager.Instance.RegisterLoot(coord, Instantiate(chestPrefab, fullMap.CellToWorld(new(coord.x, coord.y)), Quaternion.identity));
            }

            Dictionary<Vector2Int, string> enemyLocations = ExplorationManager.Instance.GetEnemies();
            foreach(Vector2Int location in enemyLocations.Keys)
            {
                currentEnemies.Add(Instantiate(enemyPrefabs[enemyNames.IndexOf(enemyLocations[location])], fullMap.CellToWorld(new(location.x, location.y)), Quaternion.identity));
            }
            Vector2Int playerCoords = ExplorationManager.Instance.GetPlayerLocation();
            player.transform.position = fullMap.CellToWorld(new(playerCoords.x, playerCoords.y));
        }
        Camera.main.transform.position = new(player.transform.position.x, player.transform.position.y, Camera.main.transform.position.z);
        validPlayerMovementLocations = ExplorationManager.Instance.GetValidPlayerMoves();
        UpdateOverlay();
    }

    private void Update()
    {
        if(stage < 2)
        {
            Vector2 screenPosition = inputActionAsset.FindAction("MousePosition", true).ReadValue<Vector2>();
            Vector2Int currentMouseCoords = (Vector2Int)fullMap.WorldToCell(Camera.main.ScreenToWorldPoint(new(screenPosition.x, screenPosition.y, Mathf.Abs(Camera.main.transform.position.z))));
            if (validPlayerMovementLocations.Contains(currentMouseCoords))
            {
                if (inputActionAsset.FindAction("Attack", true).WasPressedThisFrame())
                {
                    ExplorationManager.Instance.ActivateTile(currentMouseCoords);
                    previousMouseCoords = currentMouseCoords;
                    player.transform.position = fullMap.CellToWorld(new(currentMouseCoords.x, currentMouseCoords.y));
                    Camera.main.transform.position = new(player.transform.position.x, player.transform.position.y, Camera.main.transform.position.z);
                    validPlayerMovementLocations = ExplorationManager.Instance.GetValidPlayerMoves();
                    stage++;
                    UpdateOverlay();
                }
                else if(!previousMouseCoords.Equals(currentMouseCoords))
                {
                    previousMouseCoords = currentMouseCoords;
                    UpdateOverlay();
                }
            }
        }
        else
        {
            foreach(GameObject enemy in currentEnemies)
            {
                Vector2Int oldLocation = (Vector2Int)fullMap.WorldToCell(enemy.transform.position);
                List<Vector2Int> options = ExplorationManager.Instance.GetValidEnemyMovements(oldLocation);
                if(options.Count > 0)
                {
                    Vector2Int newLocation = options[Random.Range(0, options.Count)];
                    enemy.transform.position = fullMap.CellToWorld(new(newLocation.x, newLocation.y));
                    ExplorationManager.Instance.MoveEnemy(oldLocation, newLocation);
                    if(newLocation.Equals(ExplorationManager.Instance.GetPlayerLocation()))
                    {
                        return;
                    }
                }
            }
            stage = 0;
        }
    }

    private void UpdateOverlay()
    {
        overlayMap.ClearAllTiles();
        foreach(Vector2Int tile in validPlayerMovementLocations)
        {
            overlayMap.SetTile(new(tile.x, tile.y), availableTile);
        }
        if(!validPlayerMovementLocations.Contains(previousMouseCoords))
        {
            previousMouseCoords = validPlayerMovementLocations[0];
        }
        overlayMap.SetTile(new(previousMouseCoords.x, previousMouseCoords.y), selectedTile);
    }
}
