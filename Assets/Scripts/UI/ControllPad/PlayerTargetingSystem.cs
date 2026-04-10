using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class PlayerTargetingSystem : MonoBehaviour
{
    Camera cam;

    public GameObject moveClickEffect;

    void Awake()
    {
        cam = Camera.main;
    }

    void Update()
    {
        // ❗ CHẶN CLICK UI
        if (EventSystem.current != null)
        {
            // PC (mouse)
            if (Mouse.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            // Mobile (touch)
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                if (EventSystem.current.IsPointerOverGameObject(Touchscreen.current.primaryTouch.touchId.ReadValue()))
                    return;
            }
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            HandleTap(Mouse.current.position.ReadValue());
        }

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            HandleTap(Touchscreen.current.primaryTouch.position.ReadValue());
        }
    }

    void HandleTap(Vector2 screenPos)
    {
        CreatureBrain player = GameManager.Instance.GetPlayer();
        if (player == null) return;

        Vector3 worldPos = cam.ScreenToWorldPoint(screenPos);
        Vector2 point = new Vector2(worldPos.x, worldPos.y);

        Collider2D hit = Physics2D.OverlapPoint(point);

        if (hit != null)
        {
            CreatureBrain target = hit.GetComponentInParent<CreatureBrain>();

            if (target != null && target != player)
            {
                player.SetManualTarget(target);
                return;
            }
        }

        // ===== TAP ĐẤT =====
        player.SetMoveTarget(point);

        // spawn hiệu ứng X
        if (moveClickEffect != null)
        {
            Instantiate(moveClickEffect, point, Quaternion.identity);
        }
    }
}