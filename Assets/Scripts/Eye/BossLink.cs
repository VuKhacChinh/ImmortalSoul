using UnityEngine;

public class BossLink : MonoBehaviour
{
    public EyeController eye;

    bool triggered = false;

    public void NotifyBossDefeated()
    {
        // chống gọi nhiều lần (rất quan trọng)
        if (triggered) return;
        triggered = true;

        if (eye != null)
        {
            eye.OnBossKilled();
        }
    }
}