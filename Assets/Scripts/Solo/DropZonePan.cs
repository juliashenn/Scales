using UnityEngine;
using UnityEngine.EventSystems;

public class DropZonePan : MonoBehaviour, IDropHandler
{
    public bool isLeftPan = true;

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData == null || eventData.pointerDrag == null) return;

        DraggableWeightItem item = eventData.pointerDrag.GetComponent<DraggableWeightItem>();

        if (item != null && !item.isPlaced)
        {
            item.isPlaced = true;
            PartitionGameManager.Instance.PlaceWeightValue(item.weightValue, isLeftPan);
        }
    }
}