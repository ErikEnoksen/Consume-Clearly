using UnityEngine;
using UnityEngine.EventSystems;

public class PointerEvent : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("clicked");
    }

}
