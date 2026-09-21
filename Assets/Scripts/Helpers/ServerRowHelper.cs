#if UNITY_EDITOR
using System;
using Unity.VisualScripting;
using UnityEngine;

public class ServerRowHelper : MonoBehaviour
{
    [SerializeField] private GameObject serverRack;
    [SerializeField] private Vector3 startingPos;
    [SerializeField] private Vector3 rackRotation;
    [SerializeField] private float offset = 2.15f;
    [SerializeField] private Vector3 rowDirection;
    [SerializeField] private float num;

    [ContextMenu("Generate Server Rack Row")]
    private void Generate()
    {
        Transform rowContainer = new GameObject("Row Container").transform;
        for (int i = 0; i < num; i++)
        {
            Instantiate(serverRack, startingPos + rowDirection.normalized * (i * offset), Quaternion.Euler(rackRotation), rowContainer); 
        }
        Debug.Log($"{num} racks generated");
    }

}
#endif