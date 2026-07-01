using UnityEngine;
using UnityEngine.InputSystem;

public class AudioManager : Singleton<AudioManager>
{
    [SerializeField] AudioSource main;
    [SerializeField] AudioSource combat;
    [SerializeField] AudioSource[] clicks;
    [SerializeField] float crossFadeTime = 5f;
    [SerializeField] InputActionAsset inputActionAsset;
    private bool stopping = false;
    private bool increaseCombat = false;
    private bool increaseMain = false;
    float completion = 0;
    public void PlayCombat()
    {
        increaseCombat = true;
        increaseMain = false;
        stopping = false;
        completion = 0;
    }

    public void PlayMain()
    {
        increaseMain = true;
        increaseCombat = false;
        stopping = false;
        completion = 0;
    }

    protected override void AwakeContinued()
    {
        if(!main.isPlaying)
        {
            main.Play();
            main.volume = 0;
        }
        if(!combat.isPlaying)
        {
            combat.Play();
            combat.volume = 0;
        }
        Stop();
    }

    void Update()
    {
        completion += Time.unscaledDeltaTime / crossFadeTime;
        if (increaseCombat)
        {
            main.volume = Mathf.Min(main.volume, Mathf.Max(0, 1 - completion));
            combat.volume = Mathf.Min(1, completion);
            increaseCombat = combat.volume != 1;
        }
        else if(increaseMain)
        {
            combat.volume = Mathf.Min(combat.volume, Mathf.Max(0, 1 - completion));
            main.volume = Mathf.Min(1, completion);
            increaseMain = main.volume != 1;
        }
        else if(stopping)
        {
            if(main.volume > 0.1f)
            {
                main.volume = Mathf.Min(main.volume, Mathf.Max(0.1f, 0.9f * (1 - completion)));
            }
            else
            {
                main.volume = Mathf.Min(0.1f, 0.1f * completion);
            }
            combat.volume = Mathf.Min(combat.volume, Mathf.Max(0, 1 - completion));

            stopping = main.volume != 0.1f;
        }
        if(inputActionAsset.FindAction("Attack", true).WasPressedThisFrame())
        {
            clicks[Random.Range(0, clicks.Length)].Play();
        }
    }

    public void Stop()
    {
        increaseCombat = false;
        increaseMain = false;
        stopping = true;
        completion = 0;
    }
}
