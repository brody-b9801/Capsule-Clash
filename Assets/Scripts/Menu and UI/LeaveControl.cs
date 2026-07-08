using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LeaveControl : MonoBehaviour
{
    private RectTransform button;

    void Start() {
        button = GetComponent<RectTransform>(); 
    }

    void Update() {
        if (Input.GetMouseButton(0) && Cursor.lockState == CursorLockMode.None) {
            Vector3 mousePos = Input.mousePosition;
            Vector3[] corners = new Vector3[4];
            button.GetWorldCorners(corners);

            Vector3 topLeft = corners[1];      
            Vector3 bottomRight = corners[3];
            if (mousePos.x > topLeft.x && mousePos.x < bottomRight.x && mousePos.y < topLeft.y && mousePos.y > bottomRight.y) {
                Shooting.canShoot = false;
                Shooting.lockCursor = false;
            }
        }
    }
}
