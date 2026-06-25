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
    //public Effects effects
    public int minRange;
    public int maxRange;
    public string description;
    public float actionTime;
    public float actionCooldown;
    public ActionEffect attackEffect;

    public Attack(string name, string attackType, TargetType targetType, List<DamageDetails> damages, uint minRange, uint maxRange, string description, float actionTime, float actionCooldown, ActionEffect attackEffect)
    {
        this.name = name;
        this.attackType = attackType;
        this.targetType = targetType;
        this.damages = damages;
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
}