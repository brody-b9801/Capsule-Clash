using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EditControl : MonoBehaviour
{
    private bool move = false;

    void Update()
    {
        if (Input.GetKey(KeyCode.A))
            move = true;
        if (move) {
            transform.position = new Vector3(transform.position.x, transform.position.y + Time.deltaTime * 10, transform.position.z);
        }
    }
}
