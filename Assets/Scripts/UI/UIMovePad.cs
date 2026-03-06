using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class UIMovePad : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    RectTransform rect;
    public RectTransform handle;

    float radius;
    float handleRadius;

    Vector2 input;
    public Vector2 Input => input;

    void Start()
    {
        rect = GetComponent<RectTransform>();

        radius = rect.rect.width * 0.5f;
        handleRadius = handle.rect.width * 0.5f;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 localPoint;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rect,
            eventData.position,
            eventData.pressEventCamera,
            out localPoint
        );

        float maxDistance = radius - handleRadius;

        localPoint = Vector2.ClampMagnitude(localPoint, maxDistance);

        handle.anchoredPosition = localPoint;

        input = localPoint / maxDistance;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        handle.anchoredPosition = Vector2.zero;
        input = Vector2.zero;
    }
}
