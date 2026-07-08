using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class MenuHandler : MonoBehaviour
{
    [SerializeField] private GameObject UI;
    [SerializeField] private GameObject titleUI;
    [SerializeField] private GameObject mainCameraGun;
    [SerializeField] private GameObject titleScreen;
    [SerializeField] private GameObject startScreen;
    [SerializeField] private GameObject roomSelectionPanel;
    [SerializeField] private GameObject SettingsScreen;
    [SerializeField] private GameObject titleBg;
    [SerializeField] private GameObject usernameInput;
    [SerializeField] private GameObject instructions;

    void Start()
    {
        roomSelectionPanel.SetActive(false);
        Camera.main.transform.position = new Vector3(4f,14.6f,-26.5f);
        Camera.main.transform.localEulerAngles = new Vector3(15,-13.5f,0);
    }

    public void browseClicked()
    {
        startScreen.SetActive(false);
        roomSelectionPanel.SetActive(true);
    }

    public void browserBack()
    {
        startScreen.SetActive(true);
        roomSelectionPanel.SetActive(false);
    }

    public void settingsClicked()
    {
        startScreen.SetActive(false);
        SettingsScreen.SetActive(true);
    }

    public void settingsBack()
    {
        startScreen.SetActive(true);
        SettingsScreen.SetActive(false);
    }

    public void instructionsClicked()
    {
        startScreen.SetActive(false);
        instructions.SetActive(true);
    }

    public void instructionsBack()
    {
        startScreen.SetActive(true);
        instructions.SetActive(false);
    }

    public void titleStart()
    {
        titleScreen.SetActive(false);
        startScreen.SetActive(true);
        titleUI.SetActive(true);
        UI.SetActive(true);
        roomSelectionPanel.SetActive(false);
        SettingsScreen.SetActive(false);
        usernameInput.SetActive(true);
        StartCoroutine(titleLerp());
    }               

    IEnumerator titleLerp()
    {
        Camera cam = Camera.main;
        Vector3 startPos = cam.transform.position;
        Quaternion startRot = cam.transform.rotation;
        Vector3 endPos = new Vector3(3.42f, 10.3f, -2f);
        Quaternion endRot = Quaternion.Euler(new Vector3(8, -204.16f, 0));
        
        float time = 0f;
        float totalTime = 0.5f;

        while (time < totalTime)
        {
            float t = time / totalTime;
            t = t * t * (3f - 2f * t);

            cam.transform.position = Vector3.Lerp(startPos, endPos, t);
            cam.transform.rotation = Quaternion.Slerp(startRot, endRot, t);

            time += Time.deltaTime;
            yield return null;
        }
        cam.transform.position = endPos;
        cam.transform.rotation = endRot;
    }
                                                                                                             
}
