using UnityEngine;

public class OutOfSoulPopup : MonoBehaviour
{

    public void Show()
    {
        gameObject.SetActive(true);
        Time.timeScale = 0f; // pause game
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        Time.timeScale = 1f;
    }

    public void OnClickWatchAds()
    {
        UIController.Instance.OnWatchAds();
    }

    public void OnClickRestart()
    {
        Time.timeScale = 1f;
        Hide();

        GameManager.Instance.StartNewRun();
    }
}