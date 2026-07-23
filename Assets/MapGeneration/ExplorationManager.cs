using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    private Vector2Int playerLocation;
    private Dictionary<Vector2Int, string> trapTypes = new();

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
        if(!traps.ContainsKey(location))
        {
            traps.Add(location, trap);
            if(!trapTypes.ContainsKey(location))
            {
                trapTypes.Add(location, trap.GetTrapType());
            }
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
        return trapTypes.Keys.ToList();
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

    public void Load()
    {
        ResetFloor();
        if(navigable is null)
        {
            navigable = new List<Vector2Int>();
        }
        else
        {
            navigable.Clear();
        }
        floorGenerated = true;
        string trapLocations = PlayerPrefs.GetString("Traps");
        string navigationLocations = PlayerPrefs.GetString("Navigation");
        string enemyLocations = PlayerPrefs.GetString("Enemies");
        string lootLocations = PlayerPrefs.GetString("Loot");
        string loadedExitLocation = PlayerPrefs.GetString("ExitLocation");
        string loadedPlayerLocation = PlayerPrefs.GetString("PlayerLocation");
        floor = PlayerPrefs.GetInt("CurrentFloor");

        if (!string.IsNullOrEmpty(trapLocations))
        {
            string[] trapLocationSplit = trapLocations.Split('\n');

            foreach (string trapLocation in trapLocationSplit)
            {
                string[] trapData = trapLocation.Split('\t');
                string[] trapPlacementSplit = trapData[0].Split(",");
                Vector2Int trapPlacement = new(int.Parse(trapPlacementSplit[0]), int.Parse(trapPlacementSplit[1]));
                trapTypes.Add(trapPlacement, trapData[1]);
            }
        }

        if(!string.IsNullOrEmpty(navigationLocations))
        {
            string[] navigationLocationsSplit = navigationLocations.Split('\n');

            foreach(string navigationLocation in navigationLocationsSplit)
            {
                string[] navigationCoords = navigationLocation.Split(',');
                Vector2Int navigationPlacement = new(int.Parse(navigationCoords[0]), int.Parse(navigationCoords[1]));
                navigable.Add(navigationPlacement);
            }
        }

        if(!string.IsNullOrEmpty(enemyLocations))
        {
            string[] enemyLocationsSplit = enemyLocations.Split('\n');

            foreach(string enemyLocation in enemyLocationsSplit)
            {
                string[] enemyData = enemyLocation.Split('\t');
                string[] enemyPlacementSplit = enemyData[0].Split(',');
                Vector2Int enemyPlacement = new(int.Parse(enemyPlacementSplit[0]), int.Parse(enemyPlacementSplit[1]));
                enemies.Add(enemyPlacement, enemyData[1]);
            }
        }

        if(!string.IsNullOrEmpty(lootLocations))
        {
            string[] lootLocationsSplit = lootLocations.Split('\n');

            foreach(string lootLocation in lootLocationsSplit)
            {
                string[] lootPlacementSplit = lootLocation.Split(',');
                Vector2Int lootPlacement = new(int.Parse(lootPlacementSplit[0]), int.Parse(lootPlacementSplit[1]));
                loot.Add(lootPlacement, null);
            }
        }

        string[] exitLocationSplit = loadedExitLocation.Split(",");
        exitLocation = new(int.Parse(exitLocationSplit[0]), int.Parse(exitLocationSplit[1]));

        string[] playerLocationSplit = loadedPlayerLocation.Split(",");
        playerLocation = new(int.Parse(playerLocationSplit[0]), int.Parse(playerLocationSplit[1]));
    }

    public void Save()
    {
        StringBuilder stringBuilder = new StringBuilder();
        foreach(Vector2Int trapLocation in trapTypes.Keys)
        {
            stringBuilder.Append($"{trapLocation.x},{trapLocation.y}\t{trapTypes[trapLocation]}\n");
        }
        PlayerPrefs.SetString("Traps", stringBuilder.ToString().Trim());

        stringBuilder.Clear();
        foreach(Vector2Int navigableTile in navigable)
        {
            stringBuilder.Append($"{navigableTile.x},{navigableTile.y}\n");
        }
        PlayerPrefs.SetString("Navigation", stringBuilder.ToString().Trim());

        stringBuilder.Clear();
        foreach(Vector2Int enemyLocation in enemies.Keys)
        {
            stringBuilder.Append($"{enemyLocation.x},{enemyLocation.y}\t{enemies[enemyLocation]}\n");
        }
        PlayerPrefs.SetString("Enemies", stringBuilder.ToString().Trim());

        stringBuilder.Clear();
        foreach(Vector2Int lootLocation in loot.Keys)
        {
            stringBuilder.Append($"{lootLocation.x},{lootLocation.y}\n");
        }
        PlayerPrefs.SetString("Loot", stringBuilder.ToString().Trim());

        PlayerPrefs.SetString("ExitLocation", $"{exitLocation.x},{exitLocation.y}");
        PlayerPrefs.SetString("PlayerLocation", $"{playerLocation.x},{playerLocation.y}");

        PlayerPrefs.Save();
    }
}
