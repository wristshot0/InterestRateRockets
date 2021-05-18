using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CreateNeptune;

public class PulsateHotStreak : MonoBehaviour
{
    private void Start()
    {
        StartCoroutine(PulsateIt());   
    }

    private IEnumerator PulsateIt()
    {
        Vector2 startSize = Vector2.one;
        Vector2 endSize = Vector2.one * 1.25f;
        float timeForEachPulse = 1f;

        for (; ;)
        {
            yield return MPAction.ScaleObject(gameObject, startSize, endSize, timeForEachPulse, "easeineaseout", false, false, false, false);
            yield return MPAction.ScaleObject(gameObject, endSize, startSize, timeForEachPulse, "easeineaseout", false, false, false, false);
        }
    }
}
