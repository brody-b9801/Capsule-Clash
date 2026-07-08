using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class lineThing : MonoBehaviour
{
    [SerializeField] private LineRenderer line;
    [SerializeField] private Transform endPoint;

    void Start()
    {
        line.SetPosition(0, transform.position);
        line.SetPosition(1, endPoint.position);
    }
}
