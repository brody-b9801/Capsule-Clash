using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    private void Start() {
        healthBar = h1;
        healthBlack = healthBlackRef;
        health = healthPrev = DamageControl.Local.health.Value;
    }

    void Update() {
        if (!PlayerMovement.Local.canTakeDamage) {
            health = 180;
        } else {
            health = DamageControl.Local.health.Value;
        }
        if (health<healthPrev)
            damageAnim = true;
        healthPrev = DamageControl.Local.health.Value;
    }

    public static void updateHealth() {
	    h = DamageControl.Local.health.Value;
      	healthBar.sizeDelta = new Vector2(h, healthBar.sizeDelta.y);
    }
}
