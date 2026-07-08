using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class JSONFetcher : MonoBehaviour
{
    public static string currentVersion = "1.0.0";
    [SerializeField] private string jsonURL = "xxx";
    public GameObject newVersionScreen;

    void Start()
    {
        StartCoroutine(FetchJSON());
    }

    IEnumerator FetchJSON()
    {
        using (UnityWebRequest request = UnityWebRequest.Get(jsonURL))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error fetching JSON: " + request.error);
            }
            else
            {
                string jsonText = request.downloadHandler.text;
                Debug.Log("Received JSON: " + jsonText);

                // Optional: parse JSON
                MyData data = JsonUtility.FromJson<MyData>(jsonText);
                if (data.version != currentVersion)
                {
                    Debug.Log("data.version: " + data.version);
                    newVersionScreen.SetActive(true);
                }
            }
        }
    }
}

//{"record":{"version":"1.0.0"},"metadata":{"id":"69596670d0ea881f40522e34","private":false,"createdAt":"2026-01-03T18:56:48.151Z"}}

[System.Serializable]
public class MyData
{
    public string version;
}
