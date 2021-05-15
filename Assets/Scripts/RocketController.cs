using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RocketController : MonoBehaviour
{
    private GameManager gm;
    private Transform t;
    private Rigidbody2D rb;

    private SpriteRenderer sr;
    private Sprite[] rocketSprites;
    private float currentSpeed;
    private float startingSpeed;
    [SerializeField] private float minStartSpeed;
    [SerializeField] private float maxStartSpeed;
    [SerializeField] private float deceleration;
    public float minSpeed;
    public float maxSpeed;

    private void Awake()
    {
        t = transform;
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        sr.sprite = rocketSprites[Random.Range(0, rocketSprites.Length)];
        startingSpeed = Random.Range(minStartSpeed, maxStartSpeed);
        currentSpeed = startingSpeed;
    }

    private void Start()
    {
        gm = GameObject.FindWithTag("gm").GetComponent<GameManager>();
    }

    private void Update()
    {
        currentSpeed = Mathf.Max(currentSpeed + gm.currentVelocityAdder + deceleration, minSpeed);
    }

    private void FixedUpdate()
    {
        rb.MovePosition(rb.position + (Vector2)t.up * currentSpeed * Time.deltaTime);
    }
}
