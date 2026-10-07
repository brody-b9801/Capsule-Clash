using UnityEngine;
using TMPro;

public class ErrorScreen : MonoBehaviour
{
    [SerializeField] private RectTransform progressBar;
    [SerializeField] private float duration = 5f;
    private float timeLeft;

    void Start()
    {
        timeLeft = duration;
        SetWidth();
    }

    void Update()
    {
        timeLeft -= Time.deltaTime;
        SetWidth();
        if (timeLeft <= 0f) {
            Destroy(gameObject);
        }
    }

    public void SetMessage(string message)
    {
        GetComponentInChildren<TMP_Text>().text = message;
    }

    private void SetWidth()
    {
        float width = 180f * Mathf.Clamp01(timeLeft / duration);
        progressBar.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
    }
}
