using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CreateNeptune;
using TMPro;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    // Scoring
    public int launches = 0;
    public int crashes = 0;
    public int hotStreak = 0;
    public int gameEndingCrashes;
    [SerializeField] private Canvas gpsCanvas;
    [SerializeField] private float hotStreakSpeedMultiplier;
    public TextMeshPro hotStreakText;
    public Text launchText;
    public Text crashText;
    [SerializeField] private Text gameOverText;

    // Rocket movement
    [SerializeField] private bool phoneTesting;
    public bool playerTouching;
    private float rocketSpeed;
    [SerializeField] private float minRocketSpeed;
    private float currentSpeedMultiplier = 1f;
    [SerializeField] private float rocketSpeedIncrement;
    [SerializeField] private float timeBetweenRocketLaunches;

    // Rates that alter acceleration and deceleration characteristics of rockets
    public float ffr;
    public float risk;
    [SerializeField] private float riskErrorBuffer;
    [SerializeField] private float riskPerSecond;
    [SerializeField] private float timeBetweenRateChanges;
    private float maxRisk = 1f;
    [SerializeField] private float maxFFR;
    [SerializeField] private Text ffrText;
    [SerializeField] private Image interestRateWell;
    [SerializeField] private Image riskFill;

    // Thruster
    [SerializeField] private RectTransform upperThruster;
    [SerializeField] private RectTransform lowerThruster;
    [SerializeField] private float minUpperThrusterY;
    [SerializeField] private float maxUpperThrusterY;
    [SerializeField] private float minLowerThrusterY;
    [SerializeField] private float maxLowerThrusterY;

    // Clouds and rockets
    [SerializeField] private Sprite[] rocketSprites;
    [SerializeField] private Sprite[] cloudSprites;
    [SerializeField] private GameObject cloud;
    private List<GameObject> clouds = new List<GameObject>();
    [SerializeField] private GameObject rocket;
    private List<GameObject> rockets = new List<GameObject>();
    [SerializeField] private Transform cloudGeneratorT;
    [SerializeField] private Transform rocketGeneratorT;

    // Atmosphere that can crash rocket if going too fast
    public Transform atmosphereT;

    private LayerMask defaultLayer;
    private LayerMask uiLayer;

    // Game state
    public GameState gameState = GameState.pregame;
    [SerializeField] private Canvas startCanvas;

    public enum GameState
    {
        pregame, gameplay, endgame
    }

    private void Awake()
    {
        uiLayer = LayerMask.NameToLayer("UI");
        defaultLayer = LayerMask.NameToLayer("Default");
        CreateObjectPools();
    }

    private void CreateObjectPools()
    {
        CNExtensions.CreateObjectPool(clouds, cloud, 10, uiLayer);
        CNExtensions.CreateObjectPool(rockets, rocket, 10, defaultLayer);
    }

    private void Start()
    {
        // Start at minimum rocket speed.
        rocketSpeed = minRocketSpeed;

        StartCoroutine(BeginGame());
        StartCoroutine(GenerateClouds());
        StartCoroutine(GenerateRockets());
        StartCoroutine(FluctuateFFR());
    }

    private IEnumerator FluctuateFFR()
    {
        for (; ;)
        {
            if (gameState == GameState.gameplay)
                ChangeFFR(Random.Range(0f, maxFFR));
            else if (gameState == GameState.endgame)
                yield break;

            yield return new WaitForSeconds(timeBetweenRateChanges);
        }
    }

    private IEnumerator GenerateRockets()
    {
        for (; ;)
        {
            if (gameState == GameState.gameplay)
            {
                GameObject newRocket = CNExtensions.GetPooledObject(rockets, rocket,
                    defaultLayer, rocketGeneratorT, new Vector2(Random.Range(-1.5f, 1.5f), 0f), Quaternion.identity, false);
                RocketController rc = newRocket.GetComponent<RocketController>();
                SpriteRenderer sr = newRocket.transform.GetChild(0).GetComponent<SpriteRenderer>();
                rc.speed = rocketSpeed * (1 + currentSpeedMultiplier);
                sr.sprite = rocketSprites[Random.Range(0, rocketSprites.Length)];

                // Increase rocket speed each round.
                IncreaseRocketSpeed();
            }
            else if (gameState == GameState.endgame)
                yield break;

            yield return new WaitForSeconds(timeBetweenRocketLaunches);
        }
    }

    private IEnumerator BeginGame()
    {
        startCanvas.enabled = true;

        yield return new WaitForSeconds(3f);

        startCanvas.enabled = false;

        // Start game.
        gameState = GameState.gameplay;
    }

    private void Update()
    {
        if (gameState == GameState.gameplay)
        {
            GetInput();
        }

        AdjustControls();
    }

    private void AdjustControls()
    {
        if (playerTouching)
            risk = Mathf.Min(risk + riskPerSecond * Time.deltaTime, maxRisk);
        else
            risk = Mathf.Max(risk - riskPerSecond * Time.deltaTime, 0f);

        upperThruster.anchoredPosition = new Vector2(upperThruster.anchoredPosition.x,
            (maxUpperThrusterY - minUpperThrusterY) * risk + minUpperThrusterY);
        lowerThruster.anchoredPosition = new Vector2(lowerThruster.anchoredPosition.x,
            (maxLowerThrusterY - minLowerThrusterY) * risk + minLowerThrusterY);

        riskFill.fillAmount = risk;
        riskFill.color = Color.HSVToRGB(risk * 0.333f, 1f, 1f);
    }

    private void GetInput()
    {
#if UNITY_EDITOR
        if (!phoneTesting)
        {
            playerTouching = Input.anyKey;
        }
#endif

        if (Input.touchCount > 0)
        {
            // Get the first touch
            Touch touch = Input.GetTouch(0);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    playerTouching = true;
                    break;
                case TouchPhase.Moved:
                    playerTouching = true;
                    break;
                case TouchPhase.Stationary:
                    playerTouching = true;
                    break;
                case TouchPhase.Canceled:
                    playerTouching = false;
                    break;
                case TouchPhase.Ended:
                    playerTouching = false;
                    break;
            }
        }
    }

    private void ChangeFFR(float newFFR)
    {
        ffr = Mathf.Max(0f, Mathf.Min(newFFR, maxFFR));
        ffrText.text = (ffr * 100).ToString("0.0") + "%";
        interestRateWell.color = Color.HSVToRGB((1f - ffr / maxFFR) * 0.333f, 1f, 1f);
    }

    public bool ShouldClearAtmosphere()
    {
        float riskTaken = risk * maxFFR;
        float minBound = (maxFFR - ffr) - riskErrorBuffer;
        float maxBound = (maxFFR - ffr) + riskErrorBuffer;

        if (riskTaken > minBound && riskTaken < maxBound)
            return true;

        return false;
    }

    public void CheckHotStreak()
    {
        if (hotStreak > 4)
        {
            currentSpeedMultiplier = hotStreakSpeedMultiplier;
            hotStreakText.enabled = true;
        }
        else
        {
            currentSpeedMultiplier = 1f;
            hotStreakText.enabled = false;
        }
    }

    private void IncreaseRocketSpeed()
    {
        rocketSpeed += rocketSpeedIncrement;
    }

    private IEnumerator GenerateClouds()
    {
        for (; ;)
        {
            Vector2 offset = new Vector2(0f, Random.Range(-2f, 2.5f));

            GameObject newCloud = CNExtensions.GetPooledObject(clouds, cloud, uiLayer, cloudGeneratorT, offset, Quaternion.identity, false);
            newCloud.GetComponent<SpriteRenderer>().sprite = cloudSprites[Random.Range(0, cloudSprites.Length)];
            CloudController cloudController = newCloud.GetComponent<CloudController>();
            cloudController.passingCloud = true;
            cloudController.cloudSpeed = Random.Range(-0.5f, -0.2f);

            yield return new WaitForSeconds(2f);
        }
    }

    public IEnumerator PulsateCrashText()
    {
        Vector2 startSize = Vector2.one;
        Vector2 endSize = Vector2.one * 1.25f;
        float pulsateTime = 0.5f;

        for (; ; )
        {
            yield return MPAction.ScaleObject(crashText.gameObject, startSize, endSize, pulsateTime, "easeineaseout", false, false, false, false);
            yield return MPAction.ScaleObject(crashText.gameObject, endSize, startSize, pulsateTime, "easeineaseout", false, false, false, false);
        }
    }

    public void EndGame()
    {
        gameOverText.text = "YOU LAUNCHED " + launches + " ROCKETS!";

        gpsCanvas.enabled = true;
    }

    public void ReloadScene()
    {
        SceneManager.LoadScene(0);
    }
}
