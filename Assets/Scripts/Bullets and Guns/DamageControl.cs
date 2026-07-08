using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Alteruna;

public class DamageControl : AttributesSync
{
    [SynchronizableField] public int health = 180;
    [SerializeField] private int damage = 18;
    [SerializeField] private int playerSelfLayer;

    [SerializeField] private Alteruna.Avatar avatar;

    void Start()
    {
       if (avatar.IsOwner) {
         avatar.gameObject.layer = playerSelfLayer;
       }
    }

    public void Hit(int damageTaken) {
        health -= damageTaken;
        if (health <= 0)
        {
           Debug.Log("Die");
        }
    }

    void OnCollisionEnter(Collision collision) {
       if (collision.gameObject.CompareTag("bullet") && !avatar.IsOwner && PlayerMovement.canTakeDamage) {
          Destroy(collision.gameObject);
          health -= damage;
          HealthController.updateHealth();
       }
    }
}
