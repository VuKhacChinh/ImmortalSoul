using UnityEngine;

public class SoulManager : MonoBehaviour
{
    public static SoulManager Instance;

    public int maxSoul = 5;

    public int souls;

    bool isOutOfSoulTriggered = false;

    void Awake()
    {
        Instance = this;
    }

    public void StartRun(int startSouls)
    {
        souls = startSouls;
        isOutOfSoulTriggered = false;
        UpdateUI();
    }

    public void RefillFullSoul()
    {
        souls = maxSoul;
        isOutOfSoulTriggered = false;
        Debug.Log("Refill full soul: " + souls);

        UpdateUI();
    }

    public bool HasSoul()
    {
        return souls > 0;
    }

    public void ConsumeSoul()
    {
        souls--;
        souls = Mathf.Max(0, souls);

        Debug.Log("Soul left: " + souls);

        UpdateUI();
    }

    public void AddSoul(int amount)
    {
        souls += amount;

        Debug.Log("Soul added: +" + amount + " -> " + souls);

        UpdateUI();
    }

    void UpdateUI()
    {
        if (UIController.Instance != null)
            UIController.Instance.UpdateSoul(souls);
    }
}