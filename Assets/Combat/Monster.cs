using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Monster : MonoBehaviour, ICombatant
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth = 100f;
    [SerializeField] private float armor = 10f;
    [SerializeField] private float actionSpeed = 10f;
    [SerializeField] private float physicalSpeed = 10f;
    [SerializeField] private float aggression = 1;
    [SerializeField] private float supportiveness = 1;
    [SerializeField] private float defensiveness = 1;
    [SerializeField] private DamageType defaultDamageType = DamageType.Physical;
    [SerializeField] private ICombatantChannel deathChannel;
    [SerializeField] private GameObject healthBar;

    [SerializeField] private List<string> monsterActions = new();
    private float remainingMovement = 0;
    private float totalMovement = 0;
    private Vector3 originalPosition;
    private Vector3 nextPosition;
    private readonly string[] defaultActions = {"Move", "Wait" };

    private readonly List<EffectDetails> activeEffects = new();
    public float DealDamage(float damage, DamageType damageType, ICombatant rootSource)
    {
        if (damage < 0)
        {
            throw new ArgumentException("Damage must be no less than 0");
        }
        float actualDamage = damage * (HasEffect(EffectType.Defending) ? 0.75f : 1);
        actualDamage = Mathf.Max(actualDamage - armor, Mathf.Min(actualDamage, 1));
        currentHealth -= actualDamage;
        if (damageType == DamageType.Physical && HasEffect(EffectType.Focusing))
        {
            rootSource.DealDamage(actualDamage * 0.25f, DamageType.Ranged, this);
        }
        if (currentHealth <= 0)
        {
            Death(rootSource);
        }
        else
        {
            healthBar.transform.localScale = new Vector3(currentHealth / maxHealth, 1, 1);
            healthBar.transform.localPosition = new Vector3((-1 + currentHealth / maxHealth) * 0.5f, 0, 0);
        }
        return actualDamage;
    }
    private bool HasEffect(EffectType effect)
    {
        foreach(EffectDetails effectDetails in activeEffects)
        {
            if(effectDetails.effectType == effect)
            {
                return true;
            }
        }
        return false;
    }

    public void Death(ICombatant source)
    {
        deathChannel.Trigger(this);
        CharacterManager.Instance.ApplyExperience(maxHealth * 0.1f + armor + actionSpeed * 0.75f + physicalSpeed * 0.5f);
        Destroy(gameObject);
    }

    public float GetActionSpeed()
    {
        return actionSpeed;
    }

    public float GetPhysicalSpeed()
    {
        return physicalSpeed;
    }

    public float Heal(float healing)
    {
        if (healing < 0)
        {
            throw new ArgumentException("Healing must be no less than 0");
        }
        //Allow for easy addition of healing modifiers
        float actualHealing = healing;
        currentHealth = Mathf.Min(currentHealth + healing, maxHealth);
        healthBar.transform.localScale = new Vector3(currentHealth / maxHealth, 1, 1);
        healthBar.transform.localPosition = new Vector3((-1 + currentHealth / maxHealth) * 0.5f, 0, 0);
        return actualHealing;
    }

    public void Move(Vector3 newPosition)
    {
        nextPosition = newPosition;
        originalPosition = transform.position;
        remainingMovement = 200 / physicalSpeed;
        totalMovement = remainingMovement;
    }

    public void StartTurn()
    {
        List<ICombatant> combatants = CombatManager.Instance.GetCombatants();
        List<Vector2Int> enemyLocations = new();
        List<Vector2Int> friendlyLocations = new();
        foreach (ICombatant combatant in combatants)
        {
            Vector2Int location = CombatManager.Instance.GetCombatantTileCoordinates(combatant);
            if(combatant is not PartyCharacter)
            {
                friendlyLocations.Add(location);
            }
            else
            {
                enemyLocations.Add(location);
            }
        }
        Vector2Int thisLocation = CombatManager.Instance.GetCombatantTileCoordinates(this);
        //TODO: Apply behavior modifiers once more actions are available
        for(int actionIndex = 0; actionIndex < monsterActions.Count; actionIndex++)
        {
            Attack attack = AttackLibrary.Instance.GetAttack(monsterActions[actionIndex], ExplorationManager.Instance.GetFloor(), physicalSpeed, actionSpeed);
            List<Vector2Int> validTargetTiles = CombatManager.Instance.DetermineValidTiles(thisLocation, attack);
            List<Vector2Int> choices = new();
            foreach(Vector2Int enemyLocation in enemyLocations)
            {
                if(validTargetTiles.Contains(enemyLocation))
                {
                    choices.Add(enemyLocation);
                }
            }
            if(choices.Count > 0)
            {
                CombatManager.Instance.CommitAction(attack, this, choices[UnityEngine.Random.Range(0, choices.Count)]);
                return;
            }
        }
        Attack movementAction = AttackLibrary.Instance.GetAttack("Move", ExplorationManager.Instance.GetFloor(), physicalSpeed, actionSpeed);
        List<Vector2Int> movableLocations = CombatManager.Instance.DetermineValidTiles(thisLocation, movementAction);
        List<Vector2Int> goodMovements = new();
        int bestDistance = int.MaxValue;
        foreach(Vector2Int potentialMove in movableLocations)
        {
            foreach(Vector2Int enemyLocation in enemyLocations)
            {
                int distance = TileHelpers.GetTileDistance(potentialMove, enemyLocation);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    goodMovements.Clear();
                    goodMovements.Add(potentialMove);
                }
                //Implicit weighting by duplicating moves that get closer to more enemies
                else if(distance == bestDistance)
                {
                    goodMovements.Add(potentialMove);
                }
            }
        }
        if(goodMovements.Count > 0)
        {
            CombatManager.Instance.CommitAction(movementAction, this, goodMovements[UnityEngine.Random.Range(0, goodMovements.Count)]);
        }
        else
        {
            CombatManager.Instance.CommitAction(AttackLibrary.Instance.GetAttack("Wait", ExplorationManager.Instance.GetFloor(), physicalSpeed, actionSpeed), this, thisLocation);
        }
    }

    public void StopTurn()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (remainingMovement > 0)
        {
            remainingMovement -= Time.deltaTime * actionSpeed;
            if (remainingMovement <= 0)
            {
                remainingMovement = 0;
            }
            transform.position = nextPosition + (originalPosition - nextPosition) * (remainingMovement / totalMovement);
        }
    }


    public void ApplyEffect(EffectDetails effect)
    {
        if (UnityEngine.Random.Range(0f, 1f) <= effect.likelihood)
        {
            activeEffects.Add(effect);
        }
    }

    void ICombatant.ProgressEffects()
    {
        for(int effectIndex = 0; effectIndex < activeEffects.Count; effectIndex++)
        {
            switch (activeEffects[effectIndex].effectType)
            {
                case EffectType.Regeneration:
                    {
                        currentHealth += 5 + activeEffects[effectIndex].level;
                        break;
                    }
            }
            if (activeEffects[effectIndex].length > 1)
            {
                activeEffects[effectIndex] = new EffectDetails(activeEffects[effectIndex].effectType, activeEffects[effectIndex].level, activeEffects[effectIndex].length - 1, 1);
            }
            else
            {
                activeEffects.RemoveAt(effectIndex);
                effectIndex--;
            }
        }
    }
}
