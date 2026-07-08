using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class FaceTextToPlayer : MonoBehaviour
{
    private Transform _camTransform;

    void Start()
    {
        _camTransform = Camera.main.transform;
    }

    void Update()
    {
        transform.rotation = Quaternion.LookRotation(transform.position - _camTransform.position);
        float dist = Mathf.Abs(Vector3.Distance(transform.position, _camTransform.position));
        transform.gameObject.layer = dist > 2 ? LayerMask.NameToLayer("Default") : LayerMask.NameToLayer("UI");
    }

    public void PortalOpen()
    {
        Debug.Log("Portal Opened");
        GetComponent<TextMeshProUGUI>().text = "Enter Portal";
        GetComponent<TextMeshProUGUI>().color = Color.green;
    }

}
