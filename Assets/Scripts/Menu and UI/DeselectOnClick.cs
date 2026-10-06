using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
[DisallowMultipleComponent]
public class DeselectOnClick : MonoBehaviour, IPointerUpHandler
{
    private Button button;

    void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(Deselect);
    }

    void OnDestroy()
    {
        button.onClick.RemoveListener(Deselect);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Deselect();
    }

    private void Deselect()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem != null && eventSystem.currentSelectedGameObject == gameObject)
        {
            eventSystem.SetSelectedGameObject(null);
        }
    }
}
