using System;
using UnityEngine;
[Serializable]
public struct EffectDetails
{
    public EffectType effectType;
    public float level;
    public float length;
    public float likelihood;
    public EffectDetails(EffectType effectType, float level, float length, float likelihood)
    {
        this.effectType = effectType;
        this.level = level;
        this.length = length;
        this.likelihood = likelihood;
    }

    public readonly EffectDetails Clone()
    {
        return new EffectDetails(effectType, level, length, likelihood);
    }
}
