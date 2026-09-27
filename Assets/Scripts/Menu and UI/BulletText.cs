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
    if (Shooting.Local == null) return;
    switch (Shooting.Local.currentGun)
    {
      case Shooting.currGun.Shotgun:
        text.text = Shooting.Local.shottieNum.ToString();
        shotgun.SetActive(true);
        bullet.SetActive(false);
        break;
      case Shooting.currGun.Sniper:
        text.text = Shooting.Local.sniperNum.ToString();
        bullet.SetActive(true);
        shotgun.SetActive(false);
        break;
      default:
        text.text = Shooting.Local.reloadNum.ToString();
        bullet.SetActive(true);
        shotgun.SetActive(false);
        break;
    }
    if (roomText != null)
           roomText.text = "In Room: " + roomName;

    }
}
