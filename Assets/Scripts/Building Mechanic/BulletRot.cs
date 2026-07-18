using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletRot : MonoBehaviour
{
    [SerializeField] private Rigidbody rb;

    void Update()
    {
        transform.rotation = Quaternion.Euler(rb.linearVelocity);        
    }
}
