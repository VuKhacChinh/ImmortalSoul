using UnityEngine;

public class BossLink : MonoBehaviour
{
    public EyeController eye;

    void OnDestroy()
    {
        if (eye != null)
            eye.OnBossKilled();
    }
}