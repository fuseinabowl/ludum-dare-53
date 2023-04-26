using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Add a SingletonProvider to an object and set its `component` to expose that component as a
/// singleton object which can be retried via `SingletonProvider.Get`.
/// 
/// For example, to make a PlayerController a singleton, add SingletonController to that object,
/// set `component` to the PlayerController component, then call SingletonProvider.Get<PlayerController>();
/// </summary>
public class SingletonProvider : MonoBehaviour
{
    private static HashSet<SingletonProvider> singletons = new HashSet<SingletonProvider>();

    public Component component;

    public static T Get<T>() where T : Component
    {
        T result;
        if (!TryGet<T>(out result))
        {
            Debug.LogWarningFormat("No singleton found, did you add a SingletonProvider component?");
            return null;
        }
        return result;
    }

    public static bool TryGet<T>(out T result) where T : Component
    {
        foreach (var entry in singletons)
        {
            if (entry.TryGetComponent<T>(out result))
            {
                return true;
            }
        }

        result = default(T);
        return false;
    }


    private void Awake()
    {
        if (component == null)
        {
            Debug.LogWarning("SingletonProvider did not specify a component, it will have no effect");
            return;
        }

        if (component.gameObject != gameObject)
        {
            Debug.LogWarning("SingletonProvider component is owned by a different object, this may cause lifecycle issues");
        }

        SingletonProvider existing = null;

        foreach (var s in singletons)
        {
            if (s.component.GetType() == component.GetType())
            {
                existing = s;
                break;
            }
        }

        if (existing != null)
        {
            Debug.LogWarningFormat("SingletonProvider already has a singleton entry for {0} on {1}",
                component.GetType(), existing.gameObject.name);
            return;
        }

        singletons.Add(this);
    }

    private void OnDestroy()
    {
        singletons.Remove(this);
    }
}
