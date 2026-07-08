using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using System.Linq;

public class LoadingScreen : MonoBehaviour
{
    [SerializeField] private TMP_Text text;
    private String periods = "";

    void OnEnable()
    {
        StartCoroutine(periodControl());
    }

    void Update()
    {
        text.text = "Loading" + periods;
    }

    IEnumerator periodControl()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.4f);
            periods += ".";
            if (periods.Length > 3)
            {
                periods = "";
            }
        }
    }
}
