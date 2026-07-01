using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ExplorationManager : Singleton<ExplorationManager>
{
    private Dictionary<Vector2Int, Trap> traps = new();
    private List<Vector2Int> navigable;
    private Dictionary<Vector2Int, string> enemies = new();
    private Dictionary<Vector2Int, GameObject> loot = new();
    private Vector2Int exitLocation;
    private bool floorGenerated = false;
    private int floor = 1;
    private List<string> trapTypes;
    private Vector2Int playerLocation;

    private bool ambush = false;
    private string currentEnemy = "";
    public void ResetFloor()
    {
        traps.Clear();
        enemies.Clear();
        loot.Clear();
    }

    public int GetFloor()
    {
        return floor;
    }

    public void RegisterTrap(Vector2Int location, Trap trap)
    {
        bool isNew = traps.ContainsKey(location);
        if(isNew)
        {
            traps.Add(location, trap);
            trapTypes.Add(trap.GetTrapType());

        }
        else
        {
            traps[location] = trap;
        }
    }

    public void RegisterExit(Vector2Int newExitLocation)
    {
        exitLocation = newExitLocation;
    }

    public void RegisterEntrance(Vector2Int location)
    {
        playerLocation = location;
    }

    public void RegisterNavigable(List<Vector2Int> navigableTiles)
    {
        navigable = navigableTiles;
    }

    public void RegisterLoot(Vector2Int location, GameObject obj)
    {
        if(loot.ContainsKey(location))
        {
            loot[location] = obj;
        }
        else
        {
            loot.Add(location, obj);
        }
    }

    public bool GetFloorGenerated()
    {
        return floorGenerated;
    }

    public void SetFloorGenerated()
    {
        floorGenerated = true;
    }

    public List<Vector2Int> GetNavigableTiles()
    {
        return navigable;
    }

    public List<Vector2Int> GetTrapLocations()
    {
        return traps.Keys.ToList();
    }
    public List<Vector2Int> GetValidEnemyMovements(Vector2Int originalPosition)
    {
        List<Vector2Int> movementOptions = TileHelpers.GetAdjacentTiles(originalPosition).ToList();
        for(int tileIndex = 0; tileIndex < movementOptions.Count; tileIndex++)
        {
            if(traps.ContainsKey(movementOptions[tileIndex]) || loot.Keys.Contains(movementOptions[tileIndex]) || 
                enemies.ContainsKey(movementOptions[tileIndex]) || exitLocation.Equals(movementOptions[tileIndex]) || 
                !navigable.Contains(movementOptions[tileIndex]))
            {
                movementOptions.RemoveAt(tileIndex);
                tileIndex--;
            }
        }
        return movementOptions.Contains(playerLocation) ? new() { playerLocation } : movementOptions;
    }

    public List<Vector2Int> GetLootLocations()
    {
        return loot.Keys.ToList();
    }

    public void ActivateTile(Vector2Int tile)
    {
        playerLocation = tile;
        if(loot.TryGetValue(tile, out GameObject lootObj))
        {
            CharacterManager.Instance.ApplyExperience(100 + 10 * floor);
            loot.Remove(tile);
            Destroy(lootObj, 0.5f);
        }
        else if(enemies.ContainsKey(tile))
        {
            ambush = false;
            currentEnemy = enemies[tile];
            enemies.Remove(tile);
            SceneManager.LoadScene("CombatTestScene");
        }
        else if(traps.ContainsKey(tile))
        {
            switch(traps[tile].GetTrapType())
            {
                default:
                    {
                        for(int characterIndex = 0; characterIndex < 3; characterIndex++)
                        {
                            CharacterManager.Instance.ChangeHealth(characterIndex, CharacterManager.Instance.GetMaxHealth(characterIndex) * -0.1f);
                            traps[tile].Trigger();
                        }
                        break;
                    }
            }
            float totalHealth = 0;
            for(int characterIndex = 0; characterIndex < 3; characterIndex++)
            {
                totalHealth += CharacterManager.Instance.GetCurrentHealth(characterIndex);
            }
            if(totalHealth == 0)
            {

                PlayerPrefs.SetInt("CanLoad", 0);
                if (PlayerPrefs.GetInt("HighestLevel", 0) < CharacterManager.Instance.GetLevel())
                {
                    PlayerPrefs.SetInt("HighestLevel", CharacterManager.Instance.GetLevel());
                }
                PlayerPrefs.Save();
                ResetData();
                CharacterManager.Instance.ResetData();
                AudioManager.Instance.Stop();
                SceneManager.LoadScene("MainMenuScene");
            }
        }
        else if(exitLocation.Equals(tile))
        {
            floor++;
            ResetFloor();
            floorGenerated = false;
            SceneManager.LoadScene("MapGenerationTestScene");
        }
    }

    public string GetEnemyType()
    {
        return currentEnemy;
    }

    public bool GetAmbush()
    {
        return ambush;
    }

    public Dictionary<Vector2Int, string> GetEnemies()
    {
        return enemies;
    }

    public void RegisterEnemy(Vector2Int location, string enemyType)
    {
        enemies.Add(location, enemyType);
    }

    public void MoveEnemy(Vector2Int oldLocation, Vector2Int newLocation)
    {
        if(newLocation.Equals(playerLocation))
        {
            currentEnemy = enemies[oldLocation];
            enemies.Remove(oldLocation);
            ambush = true;
            SceneManager.LoadScene("CombatTestScene");
        }
        else
        {
            enemies.Add(newLocation, enemies[oldLocation]);
            enemies.Remove(oldLocation);
        }
    }

    public List<Vector2Int> GetValidPlayerMoves()
    {
        List<Vector2Int> results = TileHelpers.GetAdjacentTiles(playerLocation).ToList();
        for(int resultIndex = 0; resultIndex < results.Count; resultIndex++)
        {
            if (!navigable.Contains(results[resultIndex]))
            {
                results.RemoveAt(resultIndex);
                resultIndex--;
            }
        }
        return results;
    }

    public Vector2Int GetPlayerLocation()
    {
        return playerLocation;
    }

    public void ResetData()
    {
        floor = 1;
        floorGenerated = false;
    }
}
