using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunIntersect : MonoBehaviour
{
    private int previousLayer = 0;
    void OnCollisionEnter(Collision collision)
    {
        previousLayer = collision.gameObject.layer;
        collision.gameObject.layer = 2;
    }
    
    void OnCollisionExit(Collision collision)
    {
        collision.gameObject.layer = previousLayer;
    }
}
