using System.Collections.Generic;
using FishNet;
using UnityEngine;
    
public class MatchManager : MonoBehaviour
{
    private int maxPlayers;
    private Queue<int> availableMatches;
    public static MatchManager Instance { get; set; }

    void Start()
    {
        Instance = this;
        maxPlayers =  InstanceFinder.NetworkManager.gameObject.GetComponent<NetworkHudCanvases>().GetMaxLobbyMembers;
        availableMatches = new Queue<int>();
        for (int i = 0; i < maxPlayers; i++)
        {
            availableMatches.Enqueue(i);
        }
    }

    void OnDestroy()
    {
        if (Instance != null)
        {
            Instance = null;
        }
    }

    public int GetUniqueMatch()
    {
        return availableMatches.Dequeue();
    }

    public void AddMatch(int i)
    {   
        if (!availableMatches.Contains(i))
            availableMatches.Enqueue(i);
    }
}