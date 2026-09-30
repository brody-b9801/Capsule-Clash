using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunThingAnim : MonoBehaviour
{
    bool groundedChange = false;
    public static bool movingState = false;
    public static bool gunMoving = false;
    void Update() {
        if (PlayerMovement.Local == null) return;

        if (PlayerMovement.Local.isGrounded && CameraZoom.moving && !movingState) {
            movingState = true;
            gunMoving = true;
        }
        if (!CameraZoom.moving || !PlayerMovement.Local.isGrounded) {
            movingState = false;
        }
    }

    public void EndAnim1() {
        if (!movingState)
            gunMoving = false;      
    }

    private GameObject gun;

    private GameObject Gun
    {
        get
        {
            if (gun == null) gun = transform.Find("CamAKM").gameObject;
            return gun;
        }
    }

    public void enableGun()
    {
        Gun.SetActive(true);
    }

    public void disableGun()
    {
        Gun.SetActive(false);
    }
}
