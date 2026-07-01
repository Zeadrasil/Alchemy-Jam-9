using UnityEngine;

public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    [SerializeField] private bool killAttachedGameObject = false;
    private static T instance;
    public static T Instance
    {
        get
        {
            //Ensure that an instance exists before returning
            if(instance == null)
            {
                //Prioritize an existing object
                //If all else fails just make one
                instance = FindFirstObjectByType<T>() ?? new GameObject().AddComponent<T>();
            }
            return instance;
        }
    }

    //Ensure critical Awake() functionality is maintained while allowing expansion
    protected virtual void AwakeContinued()
    {
        //For extension only
    }

    private void Awake()
    {
        //If an object already exists, kill this
        //Instance code should automatically set during this check, immediately prior to it resolving.
        if(instance == null)
        {
            instance = gameObject.GetComponent<T>();
        }
        if(instance != this)
        {
            //Determine whether the entire gameobject should be killed or just the specific component
            if(killAttachedGameObject)
            {
                Destroy(gameObject);
            }
            else
            {
                Destroy(this);
            }
        }
        else
        {
            //Pass to extension enabler
            AwakeContinued();
        }
    }
}
