using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.VisualScripting;

public class upgradeManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI killPointText;
    [SerializeField] private TextMeshProUGUI upgradeText;
    [SerializeField] private GameObject upgradeWindow;
    [SerializeField] private Transform upgradeBarContainer;
    [SerializeField] private float upgradeFactor = 0.125f;
    [SerializeField] private float upgradeCost = 3;

    public static int killPoints = 100;
    public static int upgradePoints = 0;

    //Static multipliers for upgrades
    public static float speedMultiplier = 1; // Done
    public static float jumpMultiplier = 1; // Done
    public static float dashForceMultiplier = 1; // Done
    public static float dashRegenMultiplier = 1; // Done
    public static float regenSpeedMultiplier = 1; // Done
    public static float staminaRegenMultiplier = 1; // Done
    public static float fireRateMultiplier = 1; // Done
    public static float damageMultiplier = 1; // Done
    public static float reloadSpeedMultiplier = 1; // Done

    private bool openingWindow = false;
    private bool openingWindowInv = false;
    [SerializeField] private GameObject inventoryWindow;
    private RectTransform upgradeWindowRect;
    private RectTransform inventoryWindowRect;
    private Coroutine upgradeToggleCoroutine;
    private Coroutine inventoryToggleCoroutine;
    [SerializeField] private TextMeshProUGUI keyCountText;
    [SerializeField] private TextMeshProUGUI capsuleCountText;


    //array to track which upgrades have been purchased
    private int[] upgradesPurchased = new int[9];
    private RectTransform[] upgradeRects = new RectTransform[9];

    void Start()
    {
        upgradeWindowRect = upgradeWindow.GetComponent<RectTransform>();
        if (inventoryWindow != null)
            inventoryWindowRect = inventoryWindow.GetComponent<RectTransform>();
        int tracker = 0;
        foreach (Transform child in upgradeBarContainer)
        {
            Debug.Log(child.name);
            upgradeRects[tracker] = child.GetComponent<RectTransform>();
            tracker++;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            openingWindow = !openingWindow;

            if (upgradeToggleCoroutine != null)
                StopCoroutine(upgradeToggleCoroutine);

            upgradeToggleCoroutine = StartCoroutine(ToggleUpgradeWindow());
        }
        if (Input.GetKeyDown(KeyCode.I))
        {
            openingWindowInv = !openingWindowInv;

            if (inventoryToggleCoroutine != null)
                StopCoroutine(inventoryToggleCoroutine);

            inventoryToggleCoroutine = StartCoroutine(ToggleInventoryWindow());
        }

        if (upgradeWindow.activeSelf) {
            killPointText.text = upgradePoints.ToString() + "x";  
            if (upgradePoints >= upgradeCost) {  
                if (upgradesPurchased[0] < 4 && Input.GetKeyDown(KeyCode.Alpha1)) {
                    speedMultiplier += upgradeFactor * .75f;
                    upgradePoints -= (int)upgradeCost;
                    upgradesPurchased[0]++;
                } else if (upgradesPurchased[1] < 4 && Input.GetKeyDown(KeyCode.Alpha2)) {
                    jumpMultiplier += upgradeFactor;
                    upgradePoints -= (int)upgradeCost;
                    upgradesPurchased[1]++;
                } else if (upgradesPurchased[2] < 4 && Input.GetKeyDown(KeyCode.Alpha3)) {
                    dashForceMultiplier += upgradeFactor;
                    upgradePoints -= (int)upgradeCost;
                    upgradesPurchased[2]++;
                } else if (upgradesPurchased[3] < 4 && Input.GetKeyDown(KeyCode.Alpha4)) {
                    dashRegenMultiplier += upgradeFactor * 2;
                    upgradePoints -= (int)upgradeCost;
                    upgradesPurchased[3]++;
                } else if (upgradesPurchased[4] < 4 && Input.GetKeyDown(KeyCode.Alpha5)) {
                    regenSpeedMultiplier += upgradeFactor;
                    upgradePoints -= (int)upgradeCost;
                    upgradesPurchased[4]++;
                } else if (upgradesPurchased[5] < 4 && Input.GetKeyDown(KeyCode.Alpha6)) {
                    staminaRegenMultiplier += upgradeFactor;
                    upgradePoints -= (int)upgradeCost;
                    upgradesPurchased[5]++;
                } else if (upgradesPurchased[6] < 4 && Input.GetKeyDown(KeyCode.Alpha7)) {
                    fireRateMultiplier += upgradeFactor;
                    upgradePoints -= (int)upgradeCost;
                    upgradesPurchased[6]++;
                } else if (upgradesPurchased[7] < 4 && Input.GetKeyDown(KeyCode.Alpha8)) {
                    damageMultiplier += upgradeFactor;
                    upgradePoints -= (int)upgradeCost;
                    upgradesPurchased[7]++;
                } else if (upgradesPurchased[8] < 4 && Input.GetKeyDown(KeyCode.Alpha9)) {
                    reloadSpeedMultiplier += upgradeFactor;
                    upgradePoints -= (int)upgradeCost;
                    upgradesPurchased[8]++;
                }
            }
            
            // Update upgrade bars
            for (int i = 0; i < upgradesPurchased.Length; i++) {
                float fillAmount = upgradesPurchased[i] / 4f;
                RectTransform fillBar = upgradeRects[i].GetChild(2).GetComponent<RectTransform>();
                fillBar.sizeDelta = new Vector2(180 * fillAmount, fillBar.sizeDelta.y);
            }
        }

        if (inventoryWindow.activeSelf) {
            keyCountText.text = PlayerMovement.keyCount.ToString();
            capsuleCountText.text = killPoints.ToString();
        }
    }

    IEnumerator ToggleUpgradeWindow()
    {
        upgradeWindow.SetActive(true);

        float startXPos = upgradeWindowRect.anchoredPosition.x;
        float endXPos = openingWindow ? 75f : -160f;

        float elapsedTime = 0f;
        float duration = 0.25f;

        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;
            float newXPos = Mathf.Lerp(startXPos, endXPos, t);
            upgradeWindowRect.anchoredPosition =
                new Vector2(newXPos, upgradeWindowRect.anchoredPosition.y);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        upgradeWindowRect.anchoredPosition =
            new Vector2(endXPos, upgradeWindowRect.anchoredPosition.y);

        upgradeWindow.SetActive(openingWindow);
        upgradeToggleCoroutine = null;
    }
        IEnumerator ToggleInventoryWindow()
    {
        if (inventoryWindow == null) yield break;
        
        inventoryWindow.SetActive(true);

        float startXPos = inventoryWindowRect.anchoredPosition.x;
        float endXPos = openingWindowInv ? -29f : 120f;

        float elapsedTime = 0f;
        float duration = 0.25f;

        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;
            float newXPos = Mathf.Lerp(startXPos, endXPos, t);
            inventoryWindowRect.anchoredPosition =
                new Vector2(newXPos, inventoryWindowRect.anchoredPosition.y);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        inventoryWindowRect.anchoredPosition =
            new Vector2(endXPos, inventoryWindowRect.anchoredPosition.y);

        inventoryWindow.SetActive(openingWindowInv);
        inventoryToggleCoroutine = null;
    }
}