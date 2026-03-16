using UnityEngine;

public class SoulManager : MonoBehaviour
{
    public static SoulManager Instance;

    public int souls;

    void Awake()
    {
        Instance = this;
    }

    public void StartRun(int startSouls)
    {
        souls = startSouls;
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