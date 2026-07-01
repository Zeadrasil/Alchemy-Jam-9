using System.Collections.Generic;
using UnityEngine;

public class ExplorationManager : Singleton<ExplorationManager>
{
    private List<Vector2Int> traps;
    private List<Vector2Int> navigable;
    private List<Vector2Int> enemyLocations;
    private List<Vector2Int> lootLocations;
    private Vector2Int exitLocation;

    private int floor = 1;

    public int GetFloor()
    {
        return floor;
    }

    public void RegisterTraps(List<Vector2Int> newTraps)
    {
        traps = newTraps;
    }

    public void RegisterExit(Vector2Int newExitLocation)
    {
        exitLocation = newExitLocation;
    }

    public void RegisterNavigable(List<Vector2Int> navigableTiles)
    {
        navigable = navigableTiles;
    }
}
