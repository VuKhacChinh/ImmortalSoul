using UnityEngine;

public class OutOfSoulPopup : MonoBehaviour
{
    [Header("Buttons")]
    public GameObject watchButton;
    public GameObject noInternetImage;

    private bool lastInternetState;

    public void Show()
    {
        gameObject.SetActive(true);
        Time.timeScale = 0f;

        UpdateInternetUI(true); // force update lần đầu
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        Time.timeScale = 1f;
    }

    void Update()
    {
        if (!gameObject.activeSelf) return;

        bool hasInternet = Application.internetReachability != NetworkReachability.NotReachable;

        // chỉ update khi trạng thái thay đổi (tránh spam SetActive)
        if (hasInternet != lastInternetState)
        {
            UpdateInternetUI(false);
        }
    }

    void UpdateInternetUI(bool force)
    {
        bool hasInternet = Application.internetReachability != NetworkReachability.NotReachable;

        if (!force && hasInternet == lastInternetState) return;

        lastInternetState = hasInternet;

        watchButton.SetActive(hasInternet);
        noInternetImage.SetActive(!hasInternet);
    }

    public void OnClickWatchAds()
    {
        UIController.Instance.OnWatchAds();
    }
}