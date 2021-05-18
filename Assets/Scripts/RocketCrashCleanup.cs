using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CreateNeptune;

public class RocketCrashCleanup : MonoBehaviour
{
    private GameManager gm;

    private void Awake()
    {
        gm = GameObject.FindWithTag("gm").GetComponent<GameManager>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        collision.gameObject.SetActive(false);
    }
}
