using UnityEngine;
using UnityEngine.EventSystems;

public class FruitBin : MonoBehaviour, IDropHandler 
{
    public void OnDrop(PointerEventData eventData) 
    {
        if (eventData.pointerDrag != null && eventData.pointerDrag.name.Contains(this.name)) 
        {
            Destroy(eventData.pointerDrag);
        }
    }
}