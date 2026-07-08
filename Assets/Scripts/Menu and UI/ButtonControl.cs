using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonControl : MonoBehaviour
{
    public void OpenDiscord() {
        Application.OpenURL("https://discord.gg/q2YTrQv9p2");
    }

    public void OpenYT() {
        Application.OpenURL("https://www.youtube.com/channel/UCNw0dVFoDmdtHLm3JnhZzKg");
    }

    public void OpenInsta() {
        Application.OpenURL("https://www.instagram.com/capsuleclash/");
    }

    public void UpdatePage()
    {
        Application.OpenURL("https://brody-b9801.itch.io/capsule-clash");
    }
}
