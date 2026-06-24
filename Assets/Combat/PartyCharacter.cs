using JetBrains.Annotations;
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
        indicatorTriangle.enabled = !indicatorTriangle.enabled;
        indicatorTriangleBorder.enabled = !indicatorTriangleBorder.enabled;
    }

    public void Construct(Color circleColor)
    {
        //TODO: replace with actual character sprites determined by class
        character.color = circleColor;
    }

    public float GetActionSpeed()
    {
        return actionSpeed;
    }

    public float GetPhysicalSpeed()
    {
        return physicalSpeed;
    }
}
