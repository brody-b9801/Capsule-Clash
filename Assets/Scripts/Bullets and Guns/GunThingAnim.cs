using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunThingAnim : MonoBehaviour
{
    bool groundedChange = false;
    public static bool movingState = false;
    public static bool gunMoving = false;
    void Update() {
        if (PlayerMovement.isGround() && CameraZoom.moving && !movingState) {
            movingState = true;
            gunMoving = true;
        }
        if (!CameraZoom.moving || !PlayerMovement.isGround()) {
            movingState = false;
        }
    }

    public void EndAnim1() {
        if (!movingState)
            gunMoving = false;      
    }
}
