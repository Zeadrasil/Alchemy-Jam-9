using AYellowpaper.SerializedCollections;
using UnityEngine;

public class AttackLibrary : Singleton<AttackLibrary>
{
    [SerializedDictionary("Name", "Details"), SerializeField] private SerializedDictionary<string, Attack> attacks = new();


    public Attack GetAttack(string name, int level, float physicalSpeed, float actionSpeed)
    {
        Attack attack = attacks[name].Clone();
        if(name == "Move")
        {
            attack.actionTime = 200 / physicalSpeed;
            attack.actionCooldown = attack.actionTime + 20;
        }
        else if(name == "Wait")
        {
            attack.actionCooldown = actionSpeed;
        }
        else
        {
            for (int damageIndex = 0; damageIndex < attack.damages.Count; damageIndex++)
            {
                DamageDetails damage = attack.damages[damageIndex];
                damage.min *= 1 + 0.1f * (level - 1);
                damage.max *= 1 + 0.1f * (level - 1);
                attack.damages[damageIndex] = damage;
            }
            for(int effectIndex = 0;  effectIndex < attack.effects.Count; effectIndex++)
            {
                EffectDetails effect = attack.effects[effectIndex];
                effect.length *= 1 + 0.1f * (level - 1);
                effect.level = level;
            }
        }
        return attack;
    }
}
