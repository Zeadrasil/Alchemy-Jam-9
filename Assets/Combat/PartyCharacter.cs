using System.Collections.Generic;
using System;
using UnityEngine;

public class PartyCharacter : MonoBehaviour, ICombatant
{
    [SerializeField] private SpriteRenderer warriorSprite;
    [SerializeField] private SpriteRenderer archerSprite;
    [SerializeField] private SpriteRenderer priestSprite;
    [SerializeField] private SpriteRenderer indicatorTriangleBorder;
    [SerializeField] private SpriteRenderer indicatorTriangle;
    [SerializeField] private AttackEventChannel availableAttacksReceiver;
    [SerializeField] private ICombatantChannel deathChannel;
    [SerializeField] private GameObject healthBar;

    private float remainingMovement = 0;
    private float totalMovement = 0;
    private Vector3 originalPosition;
    private Vector3 nextPosition;
    private readonly List<string> defaultActions = new() { "Defend", "Move", "Wait"};
    private readonly List<EffectDetails> activeEffects = new();

    private int characterClass = 0;
    public float DealDamage(float damage, DamageType damageType, ICombatant rootSource)
    {
        if (damage < 0)
        {
            throw new ArgumentException("Damage must be no less than 0");
        }
        float actualDamage = damage * (HasEffect(EffectType.Defending) ? 0.75f : 1);
        actualDamage = Mathf.Max(actualDamage - CharacterManager.Instance.GetArmor(characterClass), Mathf.Min(actualDamage, 1));
        CharacterManager.Instance.ChangeHealth(characterClass, -actualDamage);
        if(damageType == DamageType.Physical && HasEffect(EffectType.Focusing))
        {
            rootSource.DealDamage(actualDamage * 0.25f, DamageType.Ranged, this);
        }
        if (CharacterManager.Instance.GetCurrentHealth(characterClass) <= 0)
        {
            Death(rootSource);
        }
        else
        {
            UpdateHealthBar();
        }
        return actualDamage;
    }

    private bool HasEffect(EffectType effect)
    {
        foreach (EffectDetails effectDetails in activeEffects)
        {
            if (effectDetails.effectType == effect)
            {
                return true;
            }
        }
        return false;
    }

    public void Death(ICombatant source)
    {
        deathChannel.Trigger(this);
        Destroy(gameObject);
    }

    public float Heal(float healing)
    {
        if(healing < 0)
        {
            throw new ArgumentException("Healing must be no less than 0");
        }
        //Allow for easy addition of healing modifiers
        float actualHealing = healing;
        CharacterManager.Instance.ChangeHealth(characterClass, actualHealing);
        UpdateHealthBar();
        return actualHealing;
    }

    public void StartTurn()
    {
        indicatorTriangle.enabled = true;
        indicatorTriangleBorder.enabled = true;
        foreach(string availableAction in CharacterManager.Instance.GetActions(characterClass))
        {
            availableAttacksReceiver.Trigger(AttackLibrary.Instance.GetAttack(availableAction, CharacterManager.Instance.GetLevel(), GetPhysicalSpeed(), GetActionSpeed()));
        }
        foreach(string availableAction in defaultActions)
        {
            availableAttacksReceiver.Trigger(AttackLibrary.Instance.GetAttack(availableAction, CharacterManager.Instance.GetLevel(), GetPhysicalSpeed(), GetActionSpeed()));
        }
    }

    public void Construct(int charClass)
    {
        warriorSprite.enabled = charClass == 0;
        archerSprite.enabled = charClass == 1;
        priestSprite.enabled = charClass == 2;
        if(charClass != 2)
        {
            defaultActions.Add("Focus");
        }
        characterClass = charClass;
        UpdateHealthBar();
    }

    public float GetActionSpeed()
    {
        return CharacterManager.Instance.GetActionSpeed(characterClass);
    }

    public float GetPhysicalSpeed()
    {
        return CharacterManager.Instance.GetPhysicalSpeed(characterClass);
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
        remainingMovement = 500 / GetPhysicalSpeed();
        totalMovement = remainingMovement;
    }

    public void Update()
    {
        if (remainingMovement > 0)
        {
            remainingMovement -= Time.deltaTime * GetActionSpeed();
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
        for (int effectIndex = 0; effectIndex < activeEffects.Count; effectIndex++)
        {
            switch (activeEffects[effectIndex].effectType)
            {
                case EffectType.Regeneration:
                    {
                        CharacterManager.Instance.ChangeHealth(characterClass, 5 + activeEffects[effectIndex].level);
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

    private void UpdateHealthBar()
    {
        healthBar.transform.localScale = new Vector3(CharacterManager.Instance.GetCurrentHealth(characterClass) / CharacterManager.Instance.GetMaxHealth(characterClass), 1, 1);
        healthBar.transform.localPosition = new Vector3((-1 + CharacterManager.Instance.GetCurrentHealth(characterClass) / CharacterManager.Instance.GetMaxHealth(characterClass)) * 0.5f, 0, 0);
    }
}
