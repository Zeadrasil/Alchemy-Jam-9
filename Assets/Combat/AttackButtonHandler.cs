using System.Xml.Serialization;
using TMPro;
using UnityEngine;

public class AttackButtonHandler : MonoBehaviour
{
    [SerializeField] private Attack? attackDetails;
    [SerializeField] private AttackEventChannel attackDetailsReceiver;
    [SerializeField] private TMP_Text attackNameText;

    public void SetDetails(Attack attack)
    {
        attackDetails = attack;
        attackNameText.text = attack.name;
    }

    public void SendDetails()
    {
        if (attackDetails != null && attackDetailsReceiver != null)
        {
            attackDetailsReceiver.Trigger((Attack)attackDetails);
        }
    }
}
