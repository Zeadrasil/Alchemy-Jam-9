using UnityEditor.Animations;
using UnityEngine;

public class Trap : MonoBehaviour
{
    private static readonly int StartTrapHash = Animator.StringToHash("StartTrap");
    [SerializeField] private Animator controller;
    [SerializeField] private SpriteRenderer sprite;
    [SerializeField] private string trapType;
    public void Trigger()
    {
        sprite.enabled = true;
        controller.SetTrigger(StartTrapHash);
        Destroy(gameObject, 1);
    }


    public string GetTrapType()
    {
        return trapType;
    }

}
