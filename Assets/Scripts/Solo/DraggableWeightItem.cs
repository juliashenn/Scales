using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class DraggableWeightItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public int weightValue;
    public Image shapeImage;              // The UI Image component for the shape icon
    public TextMeshProUGUI weightText;    // Text component (hidden)
    public bool isPlaced = false;

    private Transform originalParent;
    private Vector3 originalPos;
    private int originalIndex;
    private Canvas parentCanvas;
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        parentCanvas = GetComponentInParent<Canvas>();
        if (shapeImage == null) shapeImage = GetComponent<Image>();
    }

    public void Setup(int value, Sprite shapeSprite)
    {
        weightValue = value;

        // Apply visual 2D shape sprite
        if (shapeImage != null && shapeSprite != null)
        {
            shapeImage.sprite = shapeSprite;
        }

        // Hide numerical weight text so players identify weights strictly by shape
        if (weightText != null)
        {
            weightText.text = "";
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isPlaced = false;
        originalParent = transform.parent;
        originalPos = transform.position;
        originalIndex = transform.GetSiblingIndex();

        if (canvasGroup) canvasGroup.blocksRaycasts = false;
        if (parentCanvas) transform.SetParent(parentCanvas.transform, true);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (parentCanvas && !isPlaced)
        {
            transform.position = eventData.position;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isPlaced)
        {
            Destroy(gameObject);
            return;
        }

        // Return card to staging box if dropped outside a drop zone
        if (canvasGroup) canvasGroup.blocksRaycasts = true;
        if (originalParent)
        {
            transform.SetParent(originalParent);
            transform.SetSiblingIndex(originalIndex);
            transform.position = originalPos;
        }
    }
}