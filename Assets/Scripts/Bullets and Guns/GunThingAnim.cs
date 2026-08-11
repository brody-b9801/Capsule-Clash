using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunThingAnim : MonoBehaviour
{
    bool groundedChange = false;
    public static bool movingState = false;
    public static bool gunMoving = false;
    void Update() {
        // The local player is spawned by FishNet after this component starts.
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

    public void enableGun()
    {
        transform.GetChild(1).gameObject.SetActive(true);
    }

    public void disableGun()
    {
        transform.GetChild(1).gameObject.SetActive(false);
    }
}
