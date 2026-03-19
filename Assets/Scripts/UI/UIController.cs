using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class UIController : MonoBehaviour
{
    public static UIController Instance { get; private set; }

    [Header("Move UI")]
    public UIMovePad MovePad { get; private set; }

    [Header("Soul UI")]
    public TMP_Text soulText;
    public OutOfSoulPopup outOfSoulPopup;

    private CreatureBrain player;

    public Button[] skillButtons;
    CombatController combat;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        MovePad = GetComponentInChildren<UIMovePad>(true);

        for (int i = 0; i < skillButtons.Length; i++)
        {
            int index = i;

            skillButtons[i].onClick.AddListener(() =>
            {
                if (combat != null)
                    combat.UseSkill(index);
            });
        }

        outOfSoulPopup.Hide();
    }

    void Start()
    {
        UpdateSoul(SoulManager.Instance.souls);
    }

    public void SetPlayer(CreatureBrain newPlayer)
    {
        player = newPlayer;

        combat = player.GetComponent<CombatController>();

        RefreshSkillButtons();
    }

    void RefreshSkillButtons()
    {
        for (int i = 0; i < skillButtons.Length; i++)
        {
            var skill = combat.GetSkill(i);

            if (skill == null)
            {
                skillButtons[i].gameObject.SetActive(false);
            }
            else
            {
                skillButtons[i].gameObject.SetActive(true);
            }
        }
    }

    public void UpdateSoul(int value)
    {
        if (soulText != null)
            soulText.text = value.ToString();
    }

    public void ShowOutOfSoulPopup()
    {
        outOfSoulPopup.Show();
    }

    public void OnWatchAds()
    {
        bool canShowAds =
            AdsManager.Instance.IsRewardedReady &&
            AdsManager.Instance.CanShowRewarded;

        // 🔥 FALLBACK (QUAN TRỌNG NHẤT)
        if (!canShowAds)
        {
            Debug.Log("Ads cooldown → fallback revive");

            Time.timeScale = 1f;

            SoulManager.Instance.RefillFullSoul();
            outOfSoulPopup.Hide();
            GameManager.Instance.ReviveAfterAds();

            return;
        }

        // 👉 Có ads thì mới gọi
        AdsManager.Instance.ShowRewarded(
            onReward: () =>
            {
                SoulManager.Instance.RefillFullSoul();
                GameManager.Instance.ReviveAfterAds();
            },
            onClosed: () =>
            {
                Time.timeScale = 1f;
                outOfSoulPopup.Hide();
            }
        );
    }

    public void OnRestart()
    {
        GameManager.Instance.StartNewRun();
    }
    
}
