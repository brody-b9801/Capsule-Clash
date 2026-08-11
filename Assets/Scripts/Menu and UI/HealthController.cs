using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet;

public class HealthController : MonoBehaviour
{
    public static RectTransform healthBar;
    [SerializeField] private RectTransform h1;
    public static float h = 180.0f;
    public static RectTransform healthBlack;
    [SerializeField] private RectTransform healthBlackRef;
    public static bool damageAnim;
    public static bool healAnim;
    public static bool noFDAnim;
    public static bool tpAnim;
    private float health;
    private float healthPrev;
    private float lastHealth;

    private static bool PlayerReady =>
        DamageControl.Local != null && PlayerMovement.Local != null;

    private void Start() {
        healthBar = h1;
        healthBlack = healthBlackRef;
        health = healthPrev = lastHealth = 180.0f;
    }

    void Update() {
        if (!PlayerReady) return;

        float current = DamageControl.Local.health.Value;

        if (!PlayerMovement.Local.canTakeDamage) {
            health = lastHealth;
        } else {
            health = current;
            lastHealth = health;
        }

        if (health < healthPrev)
            damageAnim = true;

        healthPrev = current;
    }

    public static void updateHealth() {
        if (DamageControl.Local == null || healthBar == null) return;

        h = DamageControl.Local.health.Value;
        healthBar.sizeDelta = new Vector2(h, healthBar.sizeDelta.y);
    }
}
