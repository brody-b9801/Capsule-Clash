using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet;
using UnityEngine.UI;

public class HealthController : MonoBehaviour
{
    public static RectTransform healthBar;
    [SerializeField] private RectTransform h1;
    public static float h = 180.0f;
    public static bool damageAnim;
    public static bool healAnim;
    public static bool noFDAnim;
    public static bool tpAnim;
    private float healthPrev;
    [SerializeField] private Color invulnerableColor = new Color(1f, 0.85f, 0.2f, 1f);
    [SerializeField] private float invulnerablePulseSpeed = 4f;
    private Image healthBarImage;
    private Color healthBarColor;

    private static bool PlayerReady =>
        DamageControl.Local != null && PlayerMovement.Local != null;

    private void Start() {
        healthBar = h1;
        healthBarImage = h1 != null ? h1.GetComponent<Image>() : null;
        if (healthBarImage != null) healthBarColor = healthBarImage.color;
        healthPrev = DamageControl.MaxHealth;
        updateHealth();
    }

    void Update() {
        if (!PlayerReady) return;

        float current = DamageControl.Local.health.Value;
        if (current < healthPrev)
            damageAnim = true;

        healthPrev = current;

        if (current != h) updateHealth();

        if (healthBarImage != null) {
            healthBarImage.color = DamageControl.Local.IsInvulnerable
                ? Color.Lerp(healthBarColor, invulnerableColor, Mathf.PingPong(Time.time * invulnerablePulseSpeed, 1f))
                : healthBarColor;
        }
    }

    public static void updateHealth() {
        if (DamageControl.Local == null || healthBar == null) return;

        h = DamageControl.Local.health.Value;
        healthBar.sizeDelta = new Vector2(Mathf.Max(0f, h), healthBar.sizeDelta.y);
    }
}
