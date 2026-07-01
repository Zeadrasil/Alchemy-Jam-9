using UnityEngine;

public static class EnumHelpers
{
    public static string DamageTypeToString(DamageType type)
    {
        return type switch
        {
            DamageType.Healing => "",
            DamageType.Physical => "Physical",
            DamageType.Magical => "Magical",
            DamageType.Ranged => "Ranged",
            _ => "Error, damage type not fully added"
        };

    }

    public static bool TargetTypeDemandsUnoccupied(TargetType type)
    {
        return type switch
        {
            TargetType.SingleUnoccupied => true,
            _ => false
        };
    }

    public static bool TargetTypeDemandsUnblocked(TargetType type)
    {
        return type switch
        {
            TargetType.SingleUnoccupied => true,
            TargetType.SingleUnblocked => true,
            _ => false
        };
    }

    public static bool TargetTypeHitsAllies(TargetType type)
    {
        return type switch
        {
            TargetType.SingleAllyOrSelf => true,
            TargetType.SingleAllyOnly => true,
            _ => false
        };
    }
    public static bool TargetTypeHitsEnemies(TargetType type)
    {
        return type switch
        {
            TargetType.Self => false,
            _ => true
        };
    }
    public static bool TargetTypeHitsSelf(TargetType type)
    {
        return type switch
        {
            TargetType.Self => true,
            TargetType.SingleAllyOrSelf => true,
            _ => false
        };
    }

    //public static bool TargetTypeDemandsOccupied(TargetType type)
    //{
    //    return type switch
    //    {
    //        TargetType.SingleUnoccupied => false,
    //        TargetType.SingleUnblocked => false,
    //        _ => true
    //    };
    //}
}