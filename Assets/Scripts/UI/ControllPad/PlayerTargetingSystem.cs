using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerTargetingSystem : MonoBehaviour
{
    Camera cam;

    void Awake()
    {
        cam = Camera.main;
    }

    void Update()
    {
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
    }
}