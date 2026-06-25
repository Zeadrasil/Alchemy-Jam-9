using NUnit.Framework.Constraints;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TerrainUtils;
using UnityEngine.Tilemaps;

public class CombatManager : MonoBehaviour
{
    [SerializeField] private Tilemap combatMap;
    [SerializeField] private Tilemap overlayMap;
    [SerializeField] private TileBase targetedTile;
    [SerializeField] private TileBase targetableTile;
    [SerializeField] private TileBase navigableTile;
    [SerializeField] private GameObject partyPrefab;
    [SerializeField] private bool[] livingCharacters = { true, true, true };
    [SerializeField] private bool ambush = false;
    [SerializeField] private AttackEventChannel attackDetailsSender;
    [SerializeField] private AttackEventChannel attackSelectionSender;
    [SerializeField] private GameObject attackButtonPrefab;
    [SerializeField] private GameObject attackButtonArea;
    [SerializeField] private GameObject attacksPanel;
    [SerializeField] private GameObject attackDetails;
    [SerializeField] private InputActionAsset inputActionAsset;

    readonly List<float> actionCooldowns = new();
    readonly List<ICombatant> combatants = new();
    private int nextCombatant = 0;
    private readonly List<GameObject> attackButtons = new();
    private Attack? selectedAction = null;
    private Vector2Int previousMouseCoords = Vector2Int.zero;
    private List<Vector2Int> validTargetTiles = new();
    private readonly List<Vector2Int> currentlyTargetedTiles = new();

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
        Generate();
    }

    private void Generate()
    {
        combatants.Add(Instantiate(partyPrefab, overlayMap.CellToWorld(new Vector3Int(-2, -6)), Quaternion.identity).GetComponentInChildren<PartyCharacter>());
        (combatants[0] as PartyCharacter).Construct(Color.red);
        actionCooldowns.Add(ambush ? 100 : 1);
        combatants.Add(Instantiate(partyPrefab, overlayMap.CellToWorld(new Vector3Int(0, -6)), Quaternion.identity).GetComponentInChildren<PartyCharacter>());
        (combatants[1] as PartyCharacter).Construct(Color.blue);
        actionCooldowns.Add(ambush ? 100 : 1);
        combatants.Add(Instantiate(partyPrefab, overlayMap.CellToWorld(new Vector3Int(2, -6)), Quaternion.identity).GetComponentInChildren<PartyCharacter>());
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
                if(inputActionAsset.FindAction("Attack", true).ReadValue<bool>())
                {
                    switch(selectedAction?.attackEffect)
                    {
                        case ActionEffect.Move:
                            {
                                combatants[nextCombatant].Move(overlayMap.CellToWorld(new Vector3Int(previousMouseCoords.x, previousMouseCoords.y)));
                                break;
                            }
                        case ActionEffect.Attack:
                        case ActionEffect.Heal:
                            {
                                for(int combatant = 0; combatant < combatants.Count; combatant++)
                                {
                                    if (currentlyTargetedTiles.Contains((Vector2Int)overlayMap.WorldToCell((combatants[combatant] as MonoBehaviour).transform.position)))
                                    {
                                        if ((combatant == nextCombatant && EnumHelpers.TargetTypeHitsSelf(((Attack)selectedAction).targetType)) || 
                                            (combatant != nextCombatant && combatants[combatant] is PartyCharacter && 
                                            EnumHelpers.TargetTypeHitsAllies(((Attack)selectedAction).targetType)) || 
                                            (combatants[combatant] is not PartyCharacter && EnumHelpers.TargetTypeHitsEnemies(((Attack)selectedAction).targetType)))
                                        {
                                            foreach(DamageDetails details in ((Attack)selectedAction).damages)
                                            {
                                                if (selectedAction?.attackEffect == ActionEffect.Heal)
                                                {
                                                    combatants[combatant].Heal(UnityEngine.Random.Range(details.min, details.max));
                                                }
                                                else
                                                {
                                                    combatants[combatant].DealDamage(UnityEngine.Random.Range(details.min, details.max), details.damageType, combatants[nextCombatant]);
                                                }
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
                }
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
        combatants[nextCombatant].StopTurn();
        attacksPanel.SetActive(false);
        attackDetails.SetActive(false);
        selectedAction = null;
        DetermineNextCombatant();
        Time.timeScale = 1;
    }

    private void CreateAttackButton(Attack attack)
    {
        attacksPanel.SetActive(true);
        attackDetails.SetActive(true);
        attackButtons.Add(Instantiate(attackButtonPrefab, attackButtonArea.transform));
        attackButtons[^1].GetComponent<AttackButtonHandler>().SetDetails(attack);
    }

    private void SelectAction(Attack attack)
    {
        selectedAction = attack;
        previousMouseCoords = (Vector2Int)overlayMap.WorldToCell((combatants[nextCombatant] as MonoBehaviour).transform.position);
        DetermineValidTiles();
    }

    private void UpdateOverlay()
    {
        overlayMap.ClearAllTiles();
        foreach(Vector2Int coordinate in validTargetTiles)
        {
            overlayMap.SetTile(new Vector3Int(coordinate.x, coordinate.y), targetableTile);
        }
        switch(selectedAction?.targetType)
        {
            default:
                overlayMap.SetTile(new Vector3Int(previousMouseCoords.x, previousMouseCoords.y), targetedTile);
                break;
        }
    }

    private void DetermineValidTiles()
    {
        validTargetTiles = TileHelpers.GetAllTilesWithinRange(previousMouseCoords, (uint)selectedAction?.maxRange);
        if (selectedAction?.minRange > 0)
        {
            List<Vector2Int> invalidTargetTiles = TileHelpers.GetAllTilesWithinRange(previousMouseCoords, (uint)(selectedAction?.minRange - 1));
            while (invalidTargetTiles.Count > 0)
            {
                validTargetTiles.Remove(invalidTargetTiles[0]);
                invalidTargetTiles.RemoveAt(0);
            }
        }
        bool requiresUnblocked = EnumHelpers.TargetTypeDemandsUnblocked(((Attack)selectedAction).targetType);
        if(EnumHelpers.TargetTypeDemandsUnoccupied(((Attack)selectedAction).targetType))
        {
            foreach(ICombatant combatant in combatants)
            {
                validTargetTiles.Remove((Vector2Int)overlayMap.WorldToCell((combatant as MonoBehaviour).transform.position));
            }
        }
        for (int checkIndex = 0; checkIndex < validTargetTiles.Count; checkIndex++)
        {
            Vector2Int coords = validTargetTiles[checkIndex];
            if(requiresUnblocked && combatMap.GetTile(new Vector3Int(coords.x, coords.y)) != navigableTile)
            {
                validTargetTiles.RemoveAt(checkIndex);
                checkIndex--;
            }
        }
    }

}
