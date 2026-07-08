using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BulletText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private TextMeshProUGUI roomText;
    public static string roomName;
    [SerializeField] private GameObject bullet;  
    [SerializeField] private GameObject shotgun;


    void Update()
    {
    if (!Shooting.shotgun)
    {
      text.text = Shooting.reloadNum.ToString();
      bullet.SetActive(true);
      shotgun.SetActive(false);
    }
    else
    {
      text.text = Shooting.shottieNum.ToString();
      shotgun.SetActive(true);
      bullet.SetActive(false);
    }
    if (roomText != null)
           roomText.text = "In Room: " + roomName;

    }
}
