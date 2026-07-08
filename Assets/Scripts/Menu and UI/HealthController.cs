using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthController : MonoBehaviour
{
    public static RectTransform healthBar;
    [SerializeField] private RectTransform h1;
    public static float h = PlayerMovement.healthWidth;
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
        health = healthPrev = PlayerMovement.healthWidth;
    }

    void Update() {
        if (!PlayerMovement.canTakeDamage) {
            health = 180;
        } else {
            health = PlayerMovement.healthWidth;
        }
        if (health<healthPrev)
            damageAnim = true;
        healthPrev = PlayerMovement.healthWidth;
    }

    public static void updateHealth() {
	    h = PlayerMovement.healthWidth;
      	healthBar.sizeDelta = new Vector2(h, healthBar.sizeDelta.y);
    }
}
