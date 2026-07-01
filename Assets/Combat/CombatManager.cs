using NUnit.Framework.Constraints;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TerrainUtils;
using UnityEngine.Tilemaps;

public class CombatManager : Singleton<CombatManager>
{
    [SerializeField] private Tilemap combatMap;
    [SerializeField] private Tilemap overlayMap;
    [SerializeField] private TileBase targetedTile;
    [SerializeField] private TileBase targetableTile;
    [SerializeField] private TileBase navigableTile;
    [SerializeField] private GameObject partyPrefab;
    [SerializeField] private AttackEventChannel attackDetailsSender;
    [SerializeField] private AttackEventChannel attackSelectionSender;
    [SerializeField] private ICombatantChannel deathChannel;
    [SerializeField] private GameObject attackButtonPrefab;
    [SerializeField] private GameObject attackButtonArea;
    [SerializeField] private GameObject attacksPanel;
    [SerializeField] private GameObject attackDetails;
    [SerializeField] private InputActionAsset inputActionAsset;
    [SerializeField] private GameObject[] enemyPrefabs;
    [SerializeField] private List<string> enemyNames;

    readonly List<float> actionCooldowns = new();
    readonly List<ICombatant> combatants = new();
    private int nextCombatant = 0;
    private readonly List<GameObject> attackButtons = new();
    private Attack? selectedAction = null;
    private Vector2Int previousMouseCoords = Vector2Int.zero;
    private List<Vector2Int> validTargetTiles = new();
    private readonly List<Vector2Int> currentlyTargetedTiles = new();
    private readonly List<Vector2Int> additionalOccupiedTiles = new();
    private readonly List<Vector2Int> additionalUnoccupiedTiles = new();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (attackDetailsSender != null)
        {
            attackDetailsSender.Subscribe(CreateAttackButton);
        }
        if(attackSelectionSender != null)
        {
            attackSelectionSender.Subscribe(SelectAction);
        }
        if(deathChannel != null)
        {
            deathChannel.Subscribe(RegisterDeath);
        }
        AudioManager.Instance.PlayCombat();
        Generate();
    }

    private void Generate()
    {
        if (CharacterManager.Instance.GetCurrentHealth(0) > 0)
        {
            combatants.Add(Instantiate(partyPrefab, overlayMap.CellToWorld(new Vector3Int(-2, -6)), Quaternion.identity).GetComponentInChildren<PartyCharacter>());
            (combatants[^1] as PartyCharacter).Construct(0);
            actionCooldowns.Add(ExplorationManager.Instance.GetAmbush() ? 100 : 1);
        }
        if (CharacterManager.Instance.GetCurrentHealth(1) > 0)
        {
            combatants.Add(Instantiate(partyPrefab, overlayMap.CellToWorld(new Vector3Int(0, -6)), Quaternion.identity).GetComponentInChildren<PartyCharacter>());
            (combatants[^1] as PartyCharacter).Construct(1);
            actionCooldowns.Add(ExplorationManager.Instance.GetAmbush() ? 100 : 1);
        }
        if (CharacterManager.Instance.GetCurrentHealth(2) > 0)
        {
            combatants.Add(Instantiate(partyPrefab, overlayMap.CellToWorld(new Vector3Int(2, -6)), Quaternion.identity).GetComponentInChildren<PartyCharacter>());
            (combatants[^1] as PartyCharacter).Construct(2);
            actionCooldowns.Add(ExplorationManager.Instance.GetAmbush() ? 100 : 1);
        }

        //Generate Monsters
        Vector3Int[] spawnPoints = new Vector3Int[] { new(-3, 6), new(-1, 6), new(3, 6) };
        foreach(Vector3Int spawnPoint in spawnPoints)
        {
            if(UnityEngine.Random.Range(0f, 1f) >= 0.5f)
            {
                GameObject obj = Instantiate(enemyPrefabs[enemyNames.IndexOf(ExplorationManager.Instance.GetEnemyType())], combatMap.CellToWorld(spawnPoint), Quaternion.identity);
                combatants.Add(obj.GetComponent<Monster>());
                actionCooldowns.Add(10);
            }
        }
        combatants.Add(Instantiate(enemyPrefabs[enemyNames.IndexOf(ExplorationManager.Instance.GetEnemyType())], combatMap.CellToWorld(new(1, 6)), Quaternion.identity).GetComponent<Monster>());
        actionCooldowns.Add(10);
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
            if (actionCooldowns[nextCombatant] < 0.01f)
            {
                Time.timeScale = 0;
                combatants[nextCombatant].StartTurn();
            }
        }
        else if(selectedAction != null)
        {
            Vector2 screenPosition = inputActionAsset.FindAction("MousePosition", true).ReadValue<Vector2>();
            if (screenPosition.x > Screen.width / 1920 * 400)
            {
                Vector2Int currentPosition = (Vector2Int)overlayMap.WorldToCell(Camera.main.ScreenToWorldPoint(
                    new Vector3(screenPosition.x, screenPosition.y, Mathf.Abs(Camera.main.transform.position.z))));
                if(Vector2Int.Distance(currentPosition, previousMouseCoords) > 0.1f)
                {
                    previousMouseCoords = currentPosition;
                    if(validTargetTiles.Contains(previousMouseCoords))
                    {
                        UpdateOverlay();
                    }
                }
                if(inputActionAsset.FindAction("Attack", true).WasPressedThisFrame() && validTargetTiles.Contains(previousMouseCoords))
                {
                    CommitAction((Attack)selectedAction, combatants[nextCombatant], previousMouseCoords);
                }
            }
        }
    }

    public void EndTurn(Attack action)
    {
        actionCooldowns[nextCombatant] = action.actionCooldown;
        combatants[nextCombatant].StopTurn();
        while(attackButtons.Count > 0)
        {
            Destroy(attackButtons[0]);
            attackButtons.RemoveAt(0);
        }
        overlayMap.ClearAllTiles();
        attacksPanel.SetActive(false);
        attackDetails.SetActive(false);
        selectedAction = null;
        DetermineNextCombatant();
        Time.timeScale = 1;
    }

    private void CreateAttackButton(Attack attack)
    {
        attacksPanel.SetActive(true);
        attackButtons.Add(Instantiate(attackButtonPrefab, attackButtonArea.transform));
        attackButtons[^1].GetComponent<AttackButtonHandler>().SetDetails(attack);
    }

    private void SelectAction(Attack attack)
    {
        selectedAction = attack;
        attackDetails.SetActive(true);
        previousMouseCoords = (Vector2Int)overlayMap.WorldToCell((combatants[nextCombatant] as MonoBehaviour).transform.position);
        DetermineValidTiles(previousMouseCoords, attack);
        if(validTargetTiles.Count > 0)
        {
            previousMouseCoords = validTargetTiles[0];
        }
        UpdateOverlay();
    }

    private void UpdateOverlay()
    {
        overlayMap.ClearAllTiles();
        foreach(Vector2Int coordinate in validTargetTiles)
        {
            overlayMap.SetTile(new Vector3Int(coordinate.x, coordinate.y), targetableTile);
        }
        DetermineTargetedTiles((Attack)selectedAction, previousMouseCoords);
        foreach(Vector2Int coordinate in currentlyTargetedTiles)
        {
            overlayMap.SetTile(new Vector3Int(coordinate.x, coordinate.y), targetedTile);
        }
    }

    public List<Vector2Int> DetermineValidTiles(Vector2Int center, Attack attack)
    {
        for(int additionalUnoccupiedTileCheckIndex = 0; additionalUnoccupiedTileCheckIndex < additionalUnoccupiedTiles.Count; additionalUnoccupiedTileCheckIndex++)
        {
            bool shouldRemove = true;
            foreach(ICombatant combatant in combatants)
            {
                if (additionalUnoccupiedTiles[additionalUnoccupiedTileCheckIndex].Equals(GetCombatantTileCoordinates(combatant)))
                {
                    shouldRemove = false;
                    break;
                }
            }
            if(shouldRemove)
            {
                additionalUnoccupiedTiles.RemoveAt(additionalUnoccupiedTileCheckIndex);
                additionalUnoccupiedTileCheckIndex--;
            }
        }

        foreach(ICombatant combatant in combatants)
        {
            additionalOccupiedTiles.Remove(GetCombatantTileCoordinates(combatant));
        }

        validTargetTiles = TileHelpers.GetAllTilesWithinRange(center, (uint)attack.maxRange);
        if (attack.minRange > 0)
        {
            List<Vector2Int> invalidTargetTiles = TileHelpers.GetAllTilesWithinRange(center, (uint)(attack.minRange - 1));
            while (invalidTargetTiles.Count > 0)
            {
                validTargetTiles.Remove(invalidTargetTiles[0]);
                invalidTargetTiles.RemoveAt(0);
            }
        }
        //if(EnumHelpers.TargetTypeDemandsOccupied(attack.targetType))
        //{
        //    for(int tileIndex = 0; tileIndex < validTargetTiles.Count; tileIndex++)
        //    {
        //        bool noOccupantFound = true;
        //        foreach(ICombatant combatant in combatants)
        //        {
        //            if (validTargetTiles[tileIndex].Equals(GetCombatantTileCoordinates(combatant)))
        //            {
        //                noOccupantFound = false;
        //                break;
        //            }
        //        }
        //        noOccupantFound = noOccupantFound && !additionalOccupiedTiles.Contains(validTargetTiles[tileIndex]);
        //        if(noOccupantFound)
        //        {
        //            validTargetTiles.RemoveAt(tileIndex);
        //        }
        //    }
        //}
        //else 
        if (EnumHelpers.TargetTypeDemandsUnoccupied(attack.targetType))
        {
            foreach(ICombatant combatant in combatants)
            {
                Vector2Int cellCoords = GetCombatantTileCoordinates(combatant);
                additionalOccupiedTiles.Remove(cellCoords);
                if (!additionalUnoccupiedTiles.Contains(cellCoords))
                {
                    validTargetTiles.Remove(cellCoords);
                }
            }
            foreach(Vector2Int additionalOccupiedTile in additionalOccupiedTiles)
            {
                validTargetTiles.Remove(additionalOccupiedTile);
            }
        }
        bool requiresUnblocked = EnumHelpers.TargetTypeDemandsUnblocked(attack.targetType);
        for (int checkIndex = 0; checkIndex < validTargetTiles.Count; checkIndex++)
        {
            Vector2Int coords = validTargetTiles[checkIndex];
            if(requiresUnblocked && combatMap.GetTile(new Vector3Int(coords.x, coords.y)) != navigableTile)
            {
                validTargetTiles.RemoveAt(checkIndex);
                checkIndex--;
            }
        }
        return validTargetTiles;
    }

    public Vector2Int GetCombatantTileCoordinates(ICombatant combatant)
    {
        return (Vector2Int)overlayMap.WorldToCell((combatant as MonoBehaviour).transform.position);
    }


    private void EndCombat()
    {
        Destroy(gameObject); 
        if (attackDetailsSender != null)
        {
            attackDetailsSender.Unsubscribe(CreateAttackButton);
        }
        if (attackSelectionSender != null)
        {
            attackSelectionSender.Unsubscribe(SelectAction);
        }
        if (deathChannel != null)
        {
            deathChannel.Unsubscribe(RegisterDeath);
        }
        SceneManager.LoadScene("MapGenerationTestScene");
    }

    public List<ICombatant> GetCombatants()
    {
        return combatants;
    }

    public void CommitAction(Attack action, ICombatant source, Vector2Int target)
    {
        switch (action.attackEffect)
        {
            case ActionEffect.Move:
                {
                    source.Move(overlayMap.CellToWorld(new Vector3Int(target.x, target.y)));
                    additionalOccupiedTiles.Add(target);
                    additionalUnoccupiedTiles.Add(GetCombatantTileCoordinates(source));
                    break;
                }
            case ActionEffect.Attack:
            case ActionEffect.Heal:
                {
                    DetermineTargetedTiles(action, target);
                    for (int combatant = 0; combatant < combatants.Count; combatant++)
                    {
                        if (currentlyTargetedTiles.Contains((Vector2Int)overlayMap.WorldToCell((combatants[combatant] as MonoBehaviour).transform.position)))
                        {
                            bool targetIsParty = combatants[combatant] is PartyCharacter;
                            bool targeterIsParty = combatants[nextCombatant] is PartyCharacter;
                            bool areOpponents = (targetIsParty || targeterIsParty) && !(targeterIsParty && targetIsParty);
                            bool validFromFriendlyFire = combatant != nextCombatant && !areOpponents && EnumHelpers.TargetTypeHitsAllies(action.targetType);
                            bool validFromEnemyFire = combatant != nextCombatant  && areOpponents && EnumHelpers.TargetTypeHitsEnemies(action.targetType);
                            bool validFromSelfFire = combatant == nextCombatant && EnumHelpers.TargetTypeHitsSelf(action.targetType);
                            if (validFromSelfFire || validFromFriendlyFire || validFromEnemyFire)
                            {
                                foreach (DamageDetails details in action.damages)
                                {
                                    if (action.attackEffect == ActionEffect.Heal)
                                    {
                                        combatants[combatant].Heal(UnityEngine.Random.Range(details.min, details.max));
                                    }
                                    else
                                    {
                                        combatants[combatant].DealDamage(UnityEngine.Random.Range(details.min, details.max), details.damageType, source);
                                    }
                                }
                                foreach (EffectDetails details in action.effects)
                                {
                                    combatants[combatant].ApplyEffect(details);
                                }
                            }
                        }
                    }
                    break;
                }
            default:
                Debug.Log("Unknown ActionEffect Attempted in CombatManager");
                break;
        }
        EndTurn(action);
    }

    private void RegisterDeath(ICombatant victim)
    {
        int victimIndex = combatants.IndexOf(victim);
        combatants.RemoveAt(victimIndex);
        actionCooldowns.RemoveAt(victimIndex);
        if (combatants[0] is Monster)
        {
            EndCombat();
            PlayerPrefs.SetInt("CanLoad", 0);
            if (PlayerPrefs.GetInt("HighestLevel") < CharacterManager.Instance.GetLevel())
            {
                PlayerPrefs.SetInt("HighestLevel", CharacterManager.Instance.GetLevel());
            }
            PlayerPrefs.Save();
            CharacterManager.Instance.ResetData();
            ExplorationManager.Instance.ResetData();
        }
        else if (combatants[^1] is PartyCharacter)
        {
            EndCombat();
        }
        else if (nextCombatant > victimIndex)
        {
            nextCombatant--;
        }
        else if (nextCombatant == victimIndex)
        {
            DetermineNextCombatant();
            while (attackButtons.Count > 0)
            {
                Destroy(attackButtons[0]);
                attackButtons.RemoveAt(0);
            }
            Time.timeScale = 1;
        }
    }

    private void DetermineTargetedTiles(Attack action, Vector2Int center)
    {
        currentlyTargetedTiles.Clear();
        switch (action.targetType)
        {
            default:
                currentlyTargetedTiles.Add(center);
                break;
        }
    }

}
