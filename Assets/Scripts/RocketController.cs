using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RocketController : MonoBehaviour
{
    private GameManager gm;
    private Transform t;
    private Rigidbody2D rb;

    public float speed;
    [SerializeField] private float deathSpeed;
    [SerializeField] private float atmosphericSlowdown;
    
    private bool enteredAtmosphere = false;

    private enum RocketState
    {
        flying, clearingatmosphere, success, failure
    }

    private void Awake()
    {
        t = transform;
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        // Reset.
        enteredAtmosphere = false;
    }

    private void Start()
    {
        gm = GameObject.FindWithTag("gm").GetComponent<GameManager>();
    }

    private void FixedUpdate()
    {
        // Flying speed is first set in GameManager on spawn.
        // If the game is over, crash remaining rockets.
        if (gm.gameState == GameManager.GameState.endgame)
            speed = deathSpeed;

        rb.MovePosition(rb.position + (Vector2)t.up * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("atmosphere") && !enteredAtmosphere)
        {
            enteredAtmosphere = true;
            speed *= atmosphericSlowdown;

            DetermineLaunchOrCrash();
        }
    }

    private void DetermineLaunchOrCrash()
    {
        // Determine launch or crash.
        if (gm.ShouldClearAtmosphere())
        {
            gm.hotStreak++;
            gm.launches++;
            gm.launchText.text = gm.launches.ToString();
        }
        else
        {
            speed = deathSpeed;
            gm.hotStreak = 0;

            gm.crashes++;
            gm.crashText.text = Mathf.Min(gm.crashes, gm.gameEndingCrashes).ToString();

            if (gm.crashes == gm.gameEndingCrashes - 1)
            {
                gm.crashText.color = Color.red;

                StartCoroutine(gm.PulsateCrashText());
            }
            if (gm.crashes >= gm.gameEndingCrashes)
            {
                gm.gameState = GameManager.GameState.endgame;

                gm.EndGame();
            }
        }

        gm.CheckHotStreak();
    }
}
