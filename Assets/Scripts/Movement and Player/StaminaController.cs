using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class StaminaController : MonoBehaviour
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
    public float CurrentStamina => currentStamina;
    public float MaxStamina => maxStamina;
    private Color yellow;
    private Color red;
    private bool lowStaminaWarningPlayed = false;

    private void Awake()
    {
        Local = this;
        currentStamina = maxStamina;
        yellow = new Color32(255, 220, 105, 255);
        red = new Color32(255, 112, 112, 255);
        barImage = staminaBar.GetComponent<Image>();
        UpdateStaminaBar(maxStamina);
    }

    private void OnDestroy()
    {
        if (Local == this) Local = null;
    }

    public void UpdateStamina(PlayerMovement.MoveInput input, ref PlayerMovement.ServerOutput result, float delta, bool owner)
    {
        ref float stamina = ref result.stamina;
        if (result.isSprinting && (CameraZoom.moving || result.fastAir))
        {
            if (stamina > 0f)
            {
                stamina -= staminaDepletionRate * delta;
                zoomOut = true;
            }
        } else if (stamina < maxStamina)
        {
            stamina += staminaRegenRate * input.StaminaRegenMultiplier * delta;
            stamina = Mathf.Clamp(stamina, 0f, maxStamina);
	        zoomOut = false;
        }

        UpdateStaminaBar(stamina);

	    if (stamina <= 0.0f) {
		    result.canSprint = false;
	    } else if (stamina >= 100f) {
		    result.canSprint = true;
	    }	
    }

    //public void SetStamina(float val) => currentStamina = val;

    public void UpdateStaminaBar(float currentStamina)
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
