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
    public TextMeshPro hotStreakText;
    public Text launchText;
    public Text crashText;
    [SerializeField] private Text gameOverText;

    // Rocket movement
    [SerializeField] private bool phoneTesting;
    [SerializeField] private float timeBetweenRocketLaunches;
    private float rocketSpeed;
    [SerializeField] private float minRocketSpeed;
    [SerializeField] private float maxRocketSpeed;
    [SerializeField] private float rocketSpeedIncrement;
    [SerializeField] private float hotStreakSpeedMultiplier;
    private float currentSpeedMultiplier = 1f;
    [SerializeField] private float minSpeedToClearAtmosphere;

    // Rates that alter acceleration and deceleration characteristics of rockets
    public float ffr;
    [SerializeField] private float decelerationMultiplier;
    [SerializeField] private float timeBetweenRateChanges;
    [SerializeField] private float maxFFR;
    [SerializeField] private Text ffrText;
    [SerializeField] private Image interestRateWell;
    [SerializeField] private Image returnsFill;
    [SerializeField] private GameObject[] securityGlows;
    private IEnumerator flashSecurity;

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

        // Start with TSLA investment suggestion.
        flashSecurity = FlashSecurity(2);
        StartCoroutine(flashSecurity);

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
        AdjustControls();

        NaturalRocketSpeedErosion();
    }

    private void NaturalRocketSpeedErosion()
    {
        DecreaseRocketSpeed(rocketSpeedIncrement * 0.25f);
    }

    private void AdjustControls()
    {
        float speedRatio = rocketSpeed / maxRocketSpeed;

        upperThruster.anchoredPosition = new Vector2(upperThruster.anchoredPosition.x,
            (maxUpperThrusterY - minUpperThrusterY) * speedRatio + minUpperThrusterY);
        lowerThruster.anchoredPosition = new Vector2(lowerThruster.anchoredPosition.x,
            (maxLowerThrusterY - minLowerThrusterY) * speedRatio + minLowerThrusterY);

        returnsFill.fillAmount = speedRatio;
        returnsFill.color = Color.HSVToRGB(speedRatio * 0.333f, 1f, 1f);
    }

    private void ChangeFFR(float newFFR)
    {
        ffr = Mathf.Max(0f, Mathf.Min(newFFR, maxFFR));
        ffrText.text = (ffr * 100).ToString("0.0") + "%";
        interestRateWell.color = Color.HSVToRGB((1f - ffr / maxFFR) * 0.333f, 1f, 1f);

        // Flash glow on correct security to choose.
        if (flashSecurity != null)
            StopCoroutine(flashSecurity);

        if (ffr < maxFFR / 3f)
        {
            flashSecurity = FlashSecurity(2);
            StartCoroutine(flashSecurity);
        }
        else if (ffr < maxFFR * 2f / 3f)
        {
            flashSecurity = FlashSecurity(1);
            StartCoroutine(flashSecurity);
        }
        else
        {
            flashSecurity = FlashSecurity(0);
            StartCoroutine(flashSecurity);
        }
    }

    private IEnumerator FlashSecurity(int numSecurity)
    {
        Color realClear = new Color(1f, 1f, 1f, 0f);

        foreach (GameObject securityGlow in securityGlows)
        {
            securityGlow.GetComponent<Image>().color = realClear;
        }

        for (; ;)
        {
            yield return MPAction.FlashAnimation(securityGlows[numSecurity], 0.5f, 2f, realClear, Color.white, false, false, false);
        }
    }

    public bool ShouldClearAtmosphere()
    {
        if (rocketSpeed > minSpeedToClearAtmosphere)
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

    public void CheckSecurityChoice(int securityChoice)
    {
        float lowFFR = maxFFR / 3f;
        float highFFR = maxFFR * 2f / 3f;

        // If FFR is low and an aggressive choice is made, increase speed; etc.
        if ((ffr < lowFFR && securityChoice == 2) || (ffr > lowFFR && ffr < highFFR && securityChoice == 1)
            || (ffr > highFFR && securityChoice == 0))
        {
            IncreaseRocketSpeed();
        }
        else
        {
            DecreaseRocketSpeed(decelerationMultiplier);
        }
    }

    private void IncreaseRocketSpeed()
    {
        rocketSpeed = Mathf.Min(rocketSpeed + rocketSpeedIncrement, maxRocketSpeed);
    }

    private void DecreaseRocketSpeed(float speedLossMultiplier)
    {
        rocketSpeed = Mathf.Max(rocketSpeed - speedLossMultiplier * rocketSpeedIncrement, minRocketSpeed);
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
