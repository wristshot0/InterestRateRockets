using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CreateNeptune;

public class GameManager : MonoBehaviour
{
    public Sprite[] rocketSprites;
    [SerializeField] private GameObject rocket;
    [SerializeField] private GameObject cloud;
    private List<GameObject> rockets = new List<GameObject>();
    private List<GameObject> clouds = new List<GameObject>();
    [SerializeField] private Transform cloudGenerator;
    [SerializeField] private Transform rocketGenerator;

    private LayerMask defaultLayer;
    private LayerMask uiLayer;

    private void Awake()
    {
        defaultLayer = LayerMask.NameToLayer("Default");
        uiLayer = LayerMask.NameToLayer("UI");
        CreateObjectPools();
    }

    private void CreateObjectPools()
    {
        CNExtensions.CreateObjectPool(rockets, rocket, 10, defaultLayer);
        CNExtensions.CreateObjectPool(clouds, cloud, 10, uiLayer);
    }

    private void Start()
    {
        StartCoroutine(GenerateClouds());
    }

    private IEnumerator GenerateClouds()
    {
        for(; ;)
        {
            Vector2 offset = new Vector2(0f, Random.Range(-2f, 2.5f));

            GameObject newCloud = CNExtensions.GetPooledObject(clouds, cloud, uiLayer, cloudGenerator, offset, Quaternion.identity, false);
            CloudController cloudController = newCloud.GetComponent<CloudController>();
            SpriteRenderer newCloudSR = newCloud.GetComponent<SpriteRenderer>();
            cloudController.passingCloud = true;
            cloudController.cloudSpeed = Random.Range(-0.5f, -0.2f);

            // Front clouds
            if (Random.Range(0, 2) == 0)
            {
                newCloudSR.color = new Color(1f, 1f, 1f, Random.Range(0.15f, 0.35f));
                newCloudSR.sortingLayerName = "FrontClouds";
            }
            else
            {
                newCloudSR.color = new Color(1f, 1f, 1f, Random.Range(0.15f, 1f));
                newCloudSR.sortingLayerName = "RearClouds";
            }

            yield return new WaitForSeconds(2f);
        }
    }
}
