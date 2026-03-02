using UnityEngine;

public class PlayerControl : IControlStrategy
{
    public Vector2 GetDirection()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");
        return new Vector2(x, y).normalized;
    }

    public bool WantAttack()
    {
        return Input.GetKeyDown(KeyCode.Space);
    }
}