using UnityEngine;

public interface IControlStrategy
{
    Vector2 GetDirection();
    bool WantAttack();
}