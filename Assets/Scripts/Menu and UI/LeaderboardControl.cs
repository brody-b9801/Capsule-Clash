using Alteruna;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LeaderboardControl : AttributesSync
{
    public Alteruna.Avatar _avatar;
    public GameObject LBPrefab;
    public static Dictionary<float, List<string>> data;
    private GameObject[] taggedObjects;
    private bool playerYellowedName;
    private bool playerYellowedKC;
    public float killCount;
    public string username;
    public RectTransform lbContainer;
    public static bool dataChanged = false;
    private GameObject LB1;
    private GameObject LB2;
    private GameObject LB3;

    private GameObject[] lbEntries = new GameObject[3];

    void Start()
    {
        // Search entire hierarchy for slot objects
        LB1 = FindObjectByName("Slot 1");
        LB2 = FindObjectByName("Slot 2");
        LB3 = FindObjectByName("Slot 3");
        
        if (LB1 == null) Debug.LogError("LeaderboardControl: Could not find GameObject 'Slot 1'");
        if (LB2 == null) Debug.LogError("LeaderboardControl: Could not find GameObject 'Slot 2'");
        if (LB3 == null) Debug.LogError("LeaderboardControl: Could not find GameObject 'Slot 3'");

        lbEntries[0] = LB1;
        lbEntries[1] = LB2;
        lbEntries[2] = LB3;
        
        // Initialize avatar if not set in inspector
        if (_avatar == null)
        {
            _avatar = GetComponent<Alteruna.Avatar>();
        }
        
        UpdateLB();
        //BroadcastRemoteMethod(0);
        //lbContainer = GameObject.Find("Leaderboard").GetComponent<RectTransform>();
    }
    
    private GameObject FindObjectByName(string name)
    {
        // First try GameObject.Find (for root-level objects)
        GameObject result = GameObject.Find(name);
        if (result != null) return result;
        
        // If not found, search entire hierarchy
        foreach (GameObject obj in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (obj.name == name && obj.scene.name != null) // Only active scene objects
            {
                return obj;
            }
        }
        
        return null;
    }
    //[SynchronizableMethod]
    public void UpdateLB()
    {
        // Find and destroy existing leaderboard objects
        playerYellowedName = false;
        playerYellowedKC = false;

        // Initialize data dictionary
        data = new Dictionary<float, List<string>>();

        // Populate the dictionary with information from tagged objects
        taggedObjects = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject obj in taggedObjects)
        {
            var usernameControl = obj.GetComponent<PlayerMovement>();
            killCount = usernameControl.getKills();
            username = usernameControl.getUsername();

            // Add the username to the list for the corresponding kill count
            if (!data.ContainsKey(killCount))
            {
                data[killCount] = new List<string>();
            }
            data[killCount].Add(username);
            Debug.Log("Added " + username + " with KillCount " + killCount);
        }

        // Get a sorted list of keys (kill counts) in descending order (higher kill counts first)
        var sortedKeys = new List<float>(data.Keys);
        sortedKeys.Sort((a, b) => b.CompareTo(a));

        // Display up to four players
        int displayedCount = 0;
        for (int i = 0; i < lbEntries.Length; i++)
        {
            if (lbEntries[i] != null)
            {
                lbEntries[i].SetActive(true);
            }
            else
            {
                Debug.LogError($"LeaderboardControl: lbEntries[{i}] is null. Check that Slot {i + 1} exists in the scene.");
            }
        }

        foreach (var k in sortedKeys)
        {
            if (displayedCount >= 3) break;

            List<string> usernames = data[k];
            foreach (string u in usernames)
            {
                if (displayedCount >= 3) break;
                GameObject entry = lbEntries[displayedCount];
                
                if (entry == null)
                {
                    Debug.LogError($"LeaderboardControl: lbEntries[{displayedCount}] is null");
                    displayedCount++;
                    continue;
                }

                //RectTransform entryRect = entry.GetComponent<RectTransform>();
                //float entryHeight = entryRect.sizeDelta.y;
                
                //entry.transform.localPosition = new Vector3(0, -displayedCount * entryHeight * 1.2f, 0);   
                foreach (Transform child in entry.transform)
                {
                    //Debug.Log(child.name);
                    if (child.name == "leftText")
                    {
                        child.GetComponent<TextMeshProUGUI>().text = "#" + (displayedCount + 1).ToString() + " - " + u;
                        if (_avatar.IsOwner && !playerYellowedName)
                        {
                            playerYellowedName = true;
                            child.GetComponentInParent<Image>().color = new Color32(255, 220, 105, 255);
                            child.GetComponentInChildren<Image>().color = new Color32(255,159,53,255);
                            //child.GetComponent<TextMeshProUGUI>().color = new Color(1.0f, 0.8f, 0f, 1.0f);
                        }
                        else
                        {
                            child.GetComponentInParent<Image>().color = new Color32(255,155,100,255);
                            child.GetComponentInChildren<Image>().color = new Color32(255,100,80,255);

                            //child.GetComponent<TextMeshProUGUI>().color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
                        }
                    }
                    else if (child.name == "rightText")
                    {
                        child.GetComponent<TextMeshProUGUI>().text = ((int)k).ToString();
                        if (_avatar.IsOwner && !playerYellowedKC) {
                            playerYellowedKC = true;
                            //child.GetComponent<TextMeshProUGUI>().color = new Color(1.0f, 0.8f, 0f, 1.0f);
                        } else {
                            //child.GetComponent<TextMeshProUGUI>().color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
                        }
                    }
                }
                displayedCount++;
            }
        }
        if (displayedCount < 3)
        {
            for (int i = displayedCount; i < 3; i++)
            {
                if (lbEntries[i] != null)
                {
                    lbEntries[i].SetActive(false);
                }
            }
        }
    }
}
