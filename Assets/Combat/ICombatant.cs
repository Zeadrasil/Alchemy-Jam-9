using UnityEngine;

public interface ICombatant
{
    public float DealDamage(float damage, DamageType damageType, ICombatant rootSource);
    //public bool AddEffect(Effect effect, IDamageable source);
    public float Heal(float healing);
    public void Death(ICombatant source);

    public float GetActionSpeed();
    public float GetPhysicalSpeed();

    public void StartTurn();
}
