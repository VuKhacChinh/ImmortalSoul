using UnityEngine;

public class AIControl : IControlStrategy
{
    private float timer;
    private float interval = 2f;
    private Vector2 currentDir;

    public Vector2 GetDirection()
    {
        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            currentDir = new Vector2(
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f)
            ).normalized;

            timer = interval;
        }

        return currentDir;
    }

    public bool WantAttack()
    {
        return true;
    }
}