using UnityEngine;

public class AudioManager : Singleton<AudioManager>
{
    [SerializeField] AudioSource main;
    [SerializeField] AudioSource combat;
    [SerializeField] float crossFadeTime = 5f;
    private float realtimeAtFadeStart;
    private bool increaseCombat = false;
    private bool increaseMain = false;
    public void PlayCombat()
    {
        increaseCombat = true;
        realtimeAtFadeStart = Time.realtimeSinceStartup;
    }

    public void PlayMain()
    {
        increaseMain = true;
        realtimeAtFadeStart = Time.realtimeSinceStartup;
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
    }

    void Update()
    {
        if (increaseCombat)
        {
            float completion = (Time.realtimeSinceStartup - realtimeAtFadeStart) / crossFadeTime;
            main.volume = Mathf.Max(0, 1 - completion);
            combat.volume = Mathf.Min(1, completion);
            increaseCombat = combat.volume != 1;
        }
        else if(increaseMain)
        {
            float completion = (Time.realtimeSinceStartup - realtimeAtFadeStart) / crossFadeTime;
            combat.volume = Mathf.Max(0, 1 - completion);
            main.volume = Mathf.Min(1, completion);
            increaseMain = main.volume != 1;
        }
    }
}
