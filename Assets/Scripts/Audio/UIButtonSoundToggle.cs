using UnityEngine;
using UnityEngine.UI;

public class UIButtonSoundToggle : MonoBehaviour
{
    [Header("UI")]
    public Image icon;
    public Sprite soundOnSprite;
    public Sprite soundOffSprite;

    void Start()
    {
        RefreshIcon();
    }

    public void OnClick()
    {
        if (AudioManager.Instance == null)
            return;

        AudioManager.Instance.ToggleVolume();
        RefreshIcon();
    }

    void RefreshIcon()
    {
        if (AudioManager.Instance == null)
            return;

        bool soundOn = AudioManager.Instance.IsSoundOn();
        icon.sprite = soundOn ? soundOnSprite : soundOffSprite;
    }
}
