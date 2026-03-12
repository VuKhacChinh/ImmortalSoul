using UnityEngine;

public class EyeFormationManager : MonoBehaviour
{
    public static EyeFormationManager Instance;

    public EyeController topEyePrefab;
    public EyeController bottomEyePrefab;
    public EyeController leftEyePrefab;
    public EyeController rightEyePrefab;
    public EyeController centerEyePrefab;

    EyeController topEye;
    EyeController bottomEye;
    EyeController leftEye;
    EyeController rightEye;
    EyeController centerEye;

    int defeatedOuter = 0;

    void Awake()
    {
        Instance = this;
    }

    public void SpawnEyes(Vector2 center, float offset)
    {
        topEye = Instantiate(topEyePrefab, center + new Vector2(0, offset), Quaternion.identity);
        bottomEye = Instantiate(bottomEyePrefab, center + new Vector2(0, -offset), Quaternion.identity);
        leftEye = Instantiate(leftEyePrefab, center + new Vector2(-offset, 0), Quaternion.identity);
        rightEye = Instantiate(rightEyePrefab, center + new Vector2(offset, 0), Quaternion.identity);

        centerEye = Instantiate(centerEyePrefab, center, Quaternion.identity);
        centerEye.gameObject.SetActive(false);
    }

    public void OnOuterBossKilled(EyeController eye)
    {
        if (eye == centerEye) return;

        defeatedOuter++;

        if (defeatedOuter >= 4)
        {
            centerEye.gameObject.SetActive(true);
        }
    }
}