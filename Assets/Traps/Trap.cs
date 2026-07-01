using UnityEngine;

public class Trap : MonoBehaviour
{
    private static readonly int StartTrapHash = Animator.StringToHash("StartTrap");
    [SerializeField] private Animator controller;
    [SerializeField] private SpriteRenderer sprite;
    [SerializeField] private string trapType;
    float timer = 0;
    public void Trigger()
    {
        sprite.enabled = true;
        controller.SetTrigger(StartTrapHash);
        timer = 2f;
    }

    private void Update()
    {
        if (timer > 0)
        {
            timer -= Time.deltaTime;
            if(timer <= 0)
            {
                sprite.enabled = false;
            }
        }
    }

    public string GetTrapType()
    {
        return trapType;
    }

}
