using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HPBar : MonoBehaviour
{
    public Image fillImage;

    public TextMeshProUGUI levelText;
    public TextMeshProUGUI hpText;

    private CreatureBrain creature;

    public void Init(CreatureBrain target)
    {
        creature = target;

        SetValue(creature.currentHP / creature.maxHP);

        UpdateTexts();
    }

    void LateUpdate()
    {
        if (creature == null) return;

        // Update vị trí theo creature
        Vector3 worldPos = creature.transform.position + new Vector3(0, 1.5f, 0);
        transform.position = worldPos;

        // Update text
        UpdateTexts();
    }

    // =========================================================
    // ĐƯỢC HPBarManager GỌI
    // =========================================================
    public void SetValue(float value)
    {
        fillImage.fillAmount = value;
    }

    void UpdateTexts()
    {
        if (creature == null) return;

        if (levelText != null)
            levelText.text = $"{creature.level}";

        if (hpText != null)
        {
            int hp = Mathf.RoundToInt(creature.currentHP);
            int max = Mathf.RoundToInt(creature.maxHP);

            hpText.text = $"{hp}/{max}";
        }
    }

    public void SetColor(Color c)
    {
        fillImage.color = c;
    }

}