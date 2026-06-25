using System.Collections.Generic;
using System;
using UnityEngine;

public class PartyCharacter : MonoBehaviour, ICombatant
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth = 100f;
    [SerializeField] private float armor = 10f;
    [SerializeField] private float actionSpeed = 10f;
    [SerializeField] private float physicalSpeed = 10f;
    [SerializeField] private SpriteRenderer character;
    [SerializeField] private SpriteRenderer indicatorTriangleBorder;
    [SerializeField] private SpriteRenderer indicatorTriangle;
    [SerializeField] private AttackEventChannel availableAttacksReceiver;

    private readonly List<Attack> characterActions = new();
    private float remainingMovement = 0;
    private float totalMovement = 0;
    private Vector3 originalPosition;
    private Vector3 nextPosition;
    private readonly Attack?[] defaultActions = new Attack?[4];
    public float DealDamage(float damage, DamageType damageType, ICombatant rootSource)
    {
        if (damage < 0)
        {
            throw new ArgumentException("Damage must be no less than 0");
        }
        float actualDamage = Mathf.Max(damage - armor, Mathf.Min(damage, 1));
        currentHealth -= actualDamage;
        if (currentHealth <= 0)
        {
            Death(rootSource);
        }
        return actualDamage;
    }

    public void Death(ICombatant source)
    {
        throw new NotImplementedException();
    }

    public float Heal(float healing)
    {
        if(healing < 0)
        {
            throw new ArgumentException("Healing must be no less than 0");
        }
        //Allow for easy addition of healing modifiers
        float actualHealing = healing;
        currentHealth = Mathf.Min(currentHealth + healing, maxHealth);
        return actualHealing;
    }

    public void StartTurn()
    {
        indicatorTriangle.enabled = true;
        indicatorTriangleBorder.enabled = true;
        foreach(Attack availableAction in characterActions)
        {
            availableAttacksReceiver.Trigger(availableAction);
        }
        foreach(Attack? availableAction in defaultActions)
        {
            if(availableAction != null)
            {
                availableAttacksReceiver.Trigger((Attack)availableAction);
            }
        }
    }

    public void Construct(Color circleColor)
    {
        //TODO: replace with actual character sprites determined by class
        character.color = circleColor;
        actionSpeed += UnityEngine.Random.Range(-3, 3);
        physicalSpeed += UnityEngine.Random.Range(-3, 3);
        defaultActions[1] = new Attack("Defend", "Self, Other", TargetType.Self, new(), 0, 0, "Reduce incoming damage until next turn", 0, 70, ActionEffect.Heal);
        defaultActions[2] = new Attack("Move", "Self, Other", TargetType.SingleUnoccupied, new(), 1, 1, "Move to an adjacent tile", 1000 / physicalSpeed, 1000 / physicalSpeed + 20, ActionEffect.Move);
        defaultActions[3] = new Attack("Wait", "Self, Other", TargetType.Self, new(), 0, 0, "Wait for 1 second and take another turn", 0, actionSpeed, ActionEffect.Heal);
        defaultActions[0] = null;
    }

    public float GetActionSpeed()
    {
        return actionSpeed;
    }

    public float GetPhysicalSpeed()
    {
        return physicalSpeed;
    }

    public void StopTurn()
    {
        indicatorTriangle.enabled = false;
        indicatorTriangleBorder.enabled = false;
    }

    public void Move(Vector3 newPosition)
    {
        nextPosition = newPosition;
        originalPosition = transform.position;
        remainingMovement = ((Attack)defaultActions[3]).actionTime;
        totalMovement = remainingMovement;
    }

    public void Update()
    {
        if (remainingMovement > 0)
        {
            remainingMovement -= Time.deltaTime * actionSpeed;
            if (remainingMovement <= 0)
            {
                remainingMovement = 0;
            }
            transform.position = (nextPosition - originalPosition) * (remainingMovement / totalMovement);
        }
    }
}
