using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Bar : MonoBehaviour
{
    [Header("Background")]
    public Image backgroundImage;
    public Sprite playerBarSprite;     // 3 bars (HP MP XP)
    public Sprite creatureBarSprite;   // 1 bar (HP only)

    [Header("Bars")]
    public Image hpFill;
    public Image mpFill;
    public Image xpFill;

    [Header("Texts")]
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI hpText;

    private CreatureBrain creature;

    public void Init(CreatureBrain target)
    {
        creature = target;

        bool isPlayer = creature.isPlayerControlled;

        // đổi background
        if (backgroundImage != null)
        {
            backgroundImage.sprite = isPlayer ? playerBarSprite : creatureBarSprite;
        }

        // ======================
        // HP
        // ======================

        SetHP(creature.runtime.HP / creature.stats.maxHP);

        // ======================
        // MP
        // ======================

        if (mpFill != null)
        {
            mpFill.gameObject.SetActive(isPlayer);

            if (isPlayer)
                SetMP(creature.runtime.MP / creature.stats.maxMP);
        }

        // ======================
        // XP
        // ======================

        if (xpFill != null)
        {
            xpFill.gameObject.SetActive(isPlayer);

            if (isPlayer)
                SetXP(LevelSystem.Instance.GetXPPercent(creature));
        }

        UpdateTexts();
    }

    void LateUpdate()
    {
        if (creature == null) return;

        // follow creature
        Vector3 worldPos = creature.transform.position + new Vector3(0, 2f, 0);
        transform.position = worldPos;

        UpdateTexts();
    }

    // =========================================================
    // HP
    // =========================================================

    public void SetHP(float value)
    {
        if (hpFill != null)
            hpFill.fillAmount = value;
    }

    // =========================================================
    // MP
    // =========================================================

    public void SetMP(float value)
    {
        if (mpFill != null)
            mpFill.fillAmount = value;
    }

    public void SetMPVisible(bool visible)
    {
        if (mpFill != null)
            mpFill.gameObject.SetActive(visible);
    }

    // =========================================================
    // XP
    // =========================================================

    public void SetXP(float value)
    {
        if (xpFill != null)
            xpFill.fillAmount = value;
    }

    // =========================================================
    // TEXT
    // =========================================================

    void UpdateTexts()
    {
        if (creature == null) return;

        if (levelText != null)
            levelText.text = $"{creature.level}";

        if (hpText != null)
        {
            int hp = Mathf.RoundToInt(creature.runtime.HP);

            // chỉ hiện text cho player
            hpText.gameObject.SetActive(creature.isPlayerControlled);

            hpText.text = $"{hp}";
        }
    }

    // =========================================================
    // COLOR
    // =========================================================

    public void SetColor(Color c)
    {
        if (hpFill != null)
            hpFill.color = c;
    }

    public void UpdatePlayerState()
    {
        if (creature == null) return;

        bool isPlayer = creature.isPlayerControlled;

        // đổi background
        if (backgroundImage != null)
            backgroundImage.sprite = isPlayer ? playerBarSprite : creatureBarSprite;

        // MP
        if (mpFill != null)
            mpFill.gameObject.SetActive(isPlayer);

        // XP
        if (xpFill != null)
        {
            xpFill.gameObject.SetActive(isPlayer);

            if (isPlayer)
            {
                float xp = LevelSystem.Instance.GetXPPercent(creature);
                xpFill.fillAmount = xp;
            }
        }

        // HP text
        if (hpText != null)
            hpText.gameObject.SetActive(isPlayer);
    }

}