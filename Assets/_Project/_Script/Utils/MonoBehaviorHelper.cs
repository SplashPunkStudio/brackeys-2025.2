using System.Collections;
using UnityEngine;

public class MonoBehaviorHelper : Singleton<MonoBehaviorHelper>
{

    protected override void Init()
    {
        DontDestroyOnLoad(gameObject);
    }
    
    public static new Coroutine StartCoroutine(IEnumerator routine) => ((MonoBehaviour) Instance).StartCoroutine(routine);

    public static new void StopCoroutine(Coroutine coroutine) => ((MonoBehaviour) Instance).StopCoroutine(coroutine);

}
