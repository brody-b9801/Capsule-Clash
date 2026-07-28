using UnityEngine;
using UnityEngine.EventSystems;

public class HoverCheck : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public static bool isHovering;

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
    }
}
