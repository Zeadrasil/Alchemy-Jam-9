using System.Collections.Generic;
using System;
using UnityEngine;
using System.Text;

[Serializable]
public struct Attack
{
    public string name;
    public string attackType;
    public TargetType targetType;
    public List<DamageDetails> damages;
    public List<EffectDetails> effects;
    public int minRange;
    public int maxRange;
    public string description;
    public float actionTime;
    public float actionCooldown;
    public ActionEffect attackEffect;

    public Attack(string name, string attackType, TargetType targetType, List<DamageDetails> damages, List<EffectDetails> effects, uint minRange, uint maxRange, string description, float actionTime, float actionCooldown, ActionEffect attackEffect)
    {
        this.name = name;
        this.attackType = attackType;
        this.targetType = targetType;
        this.damages = damages;
        this.effects = effects;
        if (minRange <= maxRange)
        {
            this.minRange = (int)minRange;
            this.maxRange = (int)maxRange;
        }
        else
        {
            this.minRange = (int)maxRange;
            this.maxRange = (int)minRange;
        }
        this.description = description;
        this.actionTime = actionTime;
        this.actionCooldown = actionCooldown;
        this.attackEffect = attackEffect;
    }

    public readonly Attack Clone()
    {
        List<DamageDetails> newDamages = new();
        foreach(DamageDetails damage in damages)
        {
            newDamages.Add(damage.Clone());
        }
        List<EffectDetails> newEffects = new();
        foreach(EffectDetails effect in effects)
        {
            newEffects.Add(effect.Clone());
        }
        return new Attack(name, attackType, targetType, newDamages, newEffects, (uint)minRange, (uint)maxRange, description, actionTime, actionCooldown, attackEffect);
    }
}