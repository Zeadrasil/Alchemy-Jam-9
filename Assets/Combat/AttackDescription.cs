using System.Text;
using TMPro;
using UnityEngine;

public class AttackDescription : MonoBehaviour
{
    [SerializeField] private AttackEventChannel attackDetailsSender;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text detailsText;
    void Start()
    {
        if (attackDetailsSender != null)
        {
            attackDetailsSender.Subscribe(UpdateDetails);
        }
    }

    private void UpdateDetails(Attack details)
    {
        nameText.text = details.name;
        StringBuilder attackDamageDetails = new();
        foreach(DamageDetails damage in details.damages)
        {
            if(attackDamageDetails.Length > 0)
            {
                attackDamageDetails.Append(", ");
            }
            attackDamageDetails.Append($"{damage.min:F1}-{damage.max:F1} {EnumHelpers.DamageTypeToString(damage.damageType)}");
        }
        detailsText.text = $"Type: {details.attackType}\n{(attackDamageDetails.Length > 0 ? $"{(details.attackEffect == ActionEffect.Heal ? "Healing" : "Damage")}: {attackDamageDetails}\n": "")}Action Time: {details.actionTime:F0}\nAction Cooldown: {details.actionCooldown:F0}";
    }
}
