using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;

public class DisconnectScreen : MonoBehaviour
{
    [Serializable]
    private class WebhookPayload
    {
        public string content;
    }

    private const string WebhookResource = "ReportWebhook";

    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button reportButton;
    [SerializeField] private TMP_Text reportButtonText;
    [SerializeField] private Button closeButton;

    void Awake()
    {
        reportButton.onClick.AddListener(Report);
        closeButton.onClick.AddListener(Close);
    }

    public void SetMessage(string message)
    {
        messageText.text = message;
    }

    private void Report()
    {
        reportButton.interactable = false;
        string report = LobbyTelemetry.MarkReported();

        TextAsset webhook = Resources.Load<TextAsset>(WebhookResource);
        string url = webhook != null ? webhook.text.Trim() : "";
        if (url == "")
        {
            CopyReport(report);
            return;
        }

        reportButtonText.text = "Sending";
        StartCoroutine(SendReport(url, report));
    }

    private IEnumerator SendReport(string url, string report)
    {
        WebhookPayload payload = new WebhookPayload
        {
            content = "Disconnect report: " + LobbyTelemetry.LastReason
                + "\nVersion " + Application.version + " on " + Application.platform
        };

        WWWForm form = new WWWForm();
        form.AddField("payload_json", JsonUtility.ToJson(payload));
        form.AddBinaryData("files[0]", Encoding.UTF8.GetBytes(report), LobbyTelemetry.LastFileName, "application/json");

        using (UnityWebRequest request = UnityWebRequest.Post(url, form))
        {
            request.timeout = 15;
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                reportButtonText.text = "Sent";
            }
            else
            {
                Debug.LogWarning("Failed to send disconnect report: " + request.error);
                CopyReport(report);
            }
        }
    }

    private void CopyReport(string report)
    {
        GUIUtility.systemCopyBuffer = report;
        reportButtonText.text = "Copied";
    }

    private void Close()
    {
        Destroy(gameObject);
    }
}
