using UnityEngine;

public enum TargetType
{
    Self = -1,
    SingleUnoccupied = 0,
    SingleUnblocked = 1,
    SingleAllyOrSelf = 2,
    SingleAllyOnly = 3
}
