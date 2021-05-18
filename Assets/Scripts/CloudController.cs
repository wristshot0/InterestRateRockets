using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CreateNeptune;

public class CloudController : MonoBehaviour
{
    private Transform t;
    private SpriteRenderer sr;
    
    [SerializeField] private Vector2 minScale;
    [SerializeField] private Vector2 maxScale;
    [SerializeField] private float minTime;
    [SerializeField] private float maxTime;
    public bool passingCloud;
    public float cloudSpeed;

    private void Awake()
    {
        t = transform;
        sr = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        // Front clouds less opacity
        if (Random.Range(0, 2) == 0)
        {
            sr.color = new Color(1f, 1f, 1f, Random.Range(0.15f, 0.35f));
            sr.sortingLayerName = "FrontClouds";
        }
        else
        {
            sr.color = new Color(1f, 1f, 1f, Random.Range(0.15f, 1f));
            sr.sortingLayerName = "RearClouds";
        }

        StartCoroutine(ScaleCloud());
    }

    private IEnumerator ScaleCloud()
    {
        float scaleTime = Random.Range(minTime, maxTime);

        for (; ;)
        {
            yield return MPAction.ScaleObject(gameObject, minScale, maxScale,
                scaleTime, "easeineaseout", false, false, false, false);

            yield return MPAction.ScaleObject(gameObject, maxScale, minScale,
                scaleTime, "easeineaseout", false, false, false, false);
        }
    }

    private void Update()
    {
        if (passingCloud)
            t.position += t.right * cloudSpeed * Time.deltaTime;
    }
}
