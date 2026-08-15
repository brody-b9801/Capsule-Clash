using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using FishNet.Object;

public class StaminaController : NetworkBehaviour
{
    [SerializeField] private RectTransform staminaBar;
    [SerializeField] private RectTransform staminaBlack;

    private Image barImage;
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float staminaRegenRate = 10f;
    [SerializeField] private float staminaDepletionRate = 20f;
    public static StaminaController Local { get; private set; }

    public bool canSprint = true;
    public static bool zoomOut = false;
    private float currentStamina;
    private Color yellow;
    private Color red;
    private bool lowStaminaWarningPlayed = false;

    public override void OnStartClient()
    {
        base.OnStartClient();
        Local = this;
        currentStamina = maxStamina;
        yellow = new Color32(255, 220, 105, 255);
        red = new Color32(255, 112, 112, 255);
        barImage = staminaBar.GetComponent<Image>();
        UpdateStaminaBar();
    }

    public override void OnStopClient()
    {
        if (Local == this) Local = null;
        base.OnStopClient();
    }

    private void Update()
    {
      if (PlayerMovement.Local == null || upgradeManager.Local == null) return;

      if (PlayerMovement.Local.isSprinting && (CameraZoom.moving || PlayerMovement.Local.fastAir))
      {
        Debug.Log("Sprinting and moving or in fast air.");
        if (currentStamina > 0f)
        {
          currentStamina -= staminaDepletionRate * Time.deltaTime;
          UpdateStaminaBar();
		      zoomOut = true;
        }
      } else if (currentStamina < maxStamina)
      {
          currentStamina += staminaRegenRate * upgradeManager.Local.staminaRegenMultiplier * Time.deltaTime;
          currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
          UpdateStaminaBar();
	        zoomOut = false;
      }

	    if (currentStamina <= 0.0f) {
		    canSprint = false;
	    } else if (currentStamina >= 100f) {
		    canSprint = true;
	    }	
    }

    private void UpdateStaminaBar()
    {
        float fillPercentage = currentStamina / maxStamina;
        float newWidth = fillPercentage * 180;
        staminaBar.sizeDelta = new Vector2(newWidth, staminaBar.sizeDelta.y);
        staminaBlack.anchoredPosition = new Vector2(130-(180-newWidth), staminaBlack.anchoredPosition.y);
        if (!canSprint && !lowStaminaWarningPlayed)
        {
          StartCoroutine(staminaLowAnim());
        } 
    }

    IEnumerator staminaLowAnim()
    {
        lowStaminaWarningPlayed = true;
        float t = 0;
        while (!canSprint)
        {
            float animTimeValue = Mathf.PingPong(t, 1f);
            barImage.color = Color.Lerp(yellow, red, animTimeValue);
            t += Time.deltaTime * 2f;
            yield return null;
        }
        barImage.color = yellow;
        lowStaminaWarningPlayed = false;
    }
}
