using UnityEngine;

public class tester : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public RectTransform marker;

    void OnPreRender()
    {
        GL.Clear(true, true, Color.magenta);
    }

    void Start()
    {
        Vector2 screenPos = new Vector2(1120, 633);

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(marker.parent as RectTransform, screenPos, null, out localPoint);
        marker.anchoredPosition = localPoint;
    }
}
