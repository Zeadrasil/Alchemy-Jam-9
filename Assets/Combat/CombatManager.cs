using NUnit.Framework.Constraints;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TerrainUtils;
using UnityEngine.Tilemaps;

public class CombatManager : MonoBehaviour
{
    [SerializeField] private Tilemap combatMap;
    [SerializeField] private TileBase navigableTile;
    [SerializeField] private TileBase blockingTile;
    [SerializeField] private GameObject partyPrefab;
    [SerializeField] private bool[] livingCharacters = { true, true, true };
    [SerializeField] private bool ambush = false;

    readonly List<float> actionCooldowns = new();
    readonly List<ICombatant> combatants = new();
    private int nextCombatant = 0;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Generate();
    }

    private void Generate()
    {
        int mapSize = 7;
        List<Vector2Int> generatePrevious = new();
        List<Vector2Int> generateCurrent = new()
        {
            Vector2Int.zero
        };
        List<Vector2Int> generateNext = new();
        for(int generationIteration = 0; generationIteration < mapSize; generationIteration++)
        {
            foreach(Vector2Int current in generateCurrent)
            {
                combatMap.SetTile(new Vector3Int(current.x, current.y), navigableTile);
                Vector2Int[] adjacents = TileHelpers.GetAdjacentTiles(current);
                foreach(Vector2Int adjacent in adjacents)
                {
                    if(!(generatePrevious.Contains(adjacent) || generateCurrent.Contains(adjacent) || generateNext.Contains(adjacent)))
                    {
                        generateNext.Add(adjacent);
                    }
                }
            }
            generatePrevious = generateCurrent;
            generateCurrent = generateNext;
            generateNext = new();
        }
        foreach(Vector2Int coord in generateCurrent)
        {
            combatMap.SetTile(new Vector3Int(coord.x, coord.y), blockingTile);
        }

        combatants.Add(Instantiate(partyPrefab, combatMap.CellToWorld(new Vector3Int(-2, -6)), Quaternion.identity).GetComponentInChildren<PartyCharacter>());
        (combatants[0] as PartyCharacter).Construct(Color.red);
        actionCooldowns.Add(ambush ? 100 : 1);
        combatants.Add(Instantiate(partyPrefab, combatMap.CellToWorld(new Vector3Int(0, -6)), Quaternion.identity).GetComponentInChildren<PartyCharacter>());
        (combatants[1] as PartyCharacter).Construct(Color.blue);
        actionCooldowns.Add(ambush ? 100 : 1);
        combatants.Add(Instantiate(partyPrefab, combatMap.CellToWorld(new Vector3Int(2, -6)), Quaternion.identity).GetComponentInChildren<PartyCharacter>());
        (combatants[2] as PartyCharacter).Construct(Color.green);
        actionCooldowns.Add(ambush ? 100 : 1);

        DetermineNextCombatant();
    }

    private void DetermineNextCombatant()
    {
        nextCombatant = 0;
        for (int combatantIndex = 1; combatantIndex < combatants.Count; combatantIndex++)
        {
            if (actionCooldowns[combatantIndex] < 0)
            {
                actionCooldowns[combatantIndex] = 0.01f;
            }
            switch ((actionCooldowns[nextCombatant] / combatants[nextCombatant].GetActionSpeed()).CompareTo(actionCooldowns[combatantIndex] / combatants[combatantIndex].GetActionSpeed()))
            {
                case 1:
                    {
                        nextCombatant = combatantIndex;
                        break;
                    }
                case 0:
                    {
                        if (combatants[combatantIndex].GetActionSpeed() > combatants[nextCombatant].GetActionSpeed())
                        {
                            nextCombatant = combatantIndex;
                        }
                        break;
                    }
                default:
                    {
                        break;
                    }

            }
        }
    }

    public void Update()
    {
        if(Time.timeScale > 0)
        {
            float timeMult = actionCooldowns[nextCombatant] - combatants[nextCombatant].GetActionSpeed() * Time.deltaTime < 0 ? 
                actionCooldowns[nextCombatant] / combatants[nextCombatant].GetActionSpeed() : Time.deltaTime;
            for(int cooldownProgressionIndex = 0; cooldownProgressionIndex < combatants.Count; cooldownProgressionIndex++)
            {
                actionCooldowns[cooldownProgressionIndex] -= combatants[cooldownProgressionIndex].GetActionSpeed() * timeMult;
            }
            if(timeMult != Time.deltaTime)
            {
                combatants[nextCombatant].StartTurn();
                Time.timeScale = 0;
            }
        }
    }

    public void EndTurn(float actionCooldown)
    {
        if(actionCooldown <= 0)
        {
            throw new ArgumentException("Action cooldown must be above 0");
        }
        actionCooldowns[nextCombatant] = actionCooldown;
        combatants[nextCombatant].StartTurn();
        DetermineNextCombatant();
        Time.timeScale = 1;
    }
}
