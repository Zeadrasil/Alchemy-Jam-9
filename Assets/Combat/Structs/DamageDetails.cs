using System;
using UnityEngine;

[Serializable]
public struct DamageDetails
{
    public float min;
    public float max;
    public DamageType damageType;

    public DamageDetails(float min, float max, DamageType damageType)
    {
        if(min <= max)
        {
            this.min = min; 
            this.max = max;
        }
        else
        {
            this.min = max;
            this.max = min;
        }
        this.damageType = damageType;
    }

    public readonly DamageDetails Clone()
    {
        return new DamageDetails(min, max, damageType);
    }
}
