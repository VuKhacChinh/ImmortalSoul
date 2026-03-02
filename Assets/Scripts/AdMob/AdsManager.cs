using UnityEngine;
using GoogleMobileAds.Api;
using System;
using System.Collections;

public class AdsManager : MonoBehaviour
{
    public static AdsManager Instance { get; private set; }

    BannerView bannerView;
    InterstitialAd interstitialAd;
    RewardedAd rewardedAd;

    const string TEST_BANNER_ID = "ca-app-pub-3940256099942544/6300978111";
    const string TEST_INTERSTITIAL_ID = "ca-app-pub-3940256099942544/1033173712";
    const string TEST_REWARDED_ID = "ca-app-pub-3940256099942544/5224354917";

    bool isInterstitialLoading;
    bool isRewardedLoading;

    const float INTERSTITIAL_COOLDOWN = 200f;
    const float REWARDED_COOLDOWN = 30f;

    float lastInterstitialTime = -999f;
    float lastRewardedTime = -999f;

    bool interstitialClosed;

    // =========================
    public bool IsOnline =>
        Application.internetReachability != NetworkReachability.NotReachable;

    public bool IsInterstitialReady =>
        interstitialAd != null && interstitialAd.CanShowAd();

    public bool IsRewardedReady =>
        rewardedAd != null && rewardedAd.CanShowAd();

    public bool CanShowInterstitial =>
        Time.unscaledTime - lastInterstitialTime >= INTERSTITIAL_COOLDOWN;

    public bool CanShowRewarded =>
        Time.unscaledTime - lastRewardedTime >= REWARDED_COOLDOWN;

    public float RewardedCooldownRemaining =>
        Mathf.Max(0f, REWARDED_COOLDOWN - (Time.unscaledTime - lastRewardedTime));

    // =========================
    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        MobileAds.Initialize(_ =>
        {
            CreateBanner();
            TryLoadAllAds();
        });

        StartCoroutine(NetworkWatcher());
    }

    // =========================
    IEnumerator NetworkWatcher()
    {
        bool lastOnline = IsOnline;

        while (true)
        {
            if (!lastOnline && IsOnline)
            {
                Debug.Log("🌐 Network back online → reload ads");
                TryLoadAllAds();
            }

            lastOnline = IsOnline;
            yield return new WaitForSecondsRealtime(1f);
        }
    }

    void TryLoadAllAds()
    {
        if (!IsOnline) return;

        if (interstitialAd == null && !isInterstitialLoading)
            LoadInterstitial();

        if (rewardedAd == null && !isRewardedLoading)
            LoadRewarded();
    }

    // =====================================================
    // ======================= BANNER ======================
    void CreateBanner()
    {
        bannerView = new BannerView(
            TEST_BANNER_ID,
            AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(
                AdSize.FullWidth),
            AdPosition.Bottom
        );

        bannerView.LoadAd(new AdRequest());
        bannerView.Hide();
    }

    public void ShowBanner()
    {
        if (IsOnline)
            bannerView?.Show();
    }

    public void HideBanner() => bannerView?.Hide();

    // =====================================================
    // =================== INTERSTITIAL ====================
    void LoadInterstitial()
    {
        if (isInterstitialLoading || !IsOnline) return;

        isInterstitialLoading = true;

        InterstitialAd.Load(
            TEST_INTERSTITIAL_ID,
            new AdRequest(),
            (ad, error) =>
            {
                isInterstitialLoading = false;

                if (error != null || ad == null)
                {
                    Debug.LogWarning("❌ Interstitial load failed");
                    return;
                }

                interstitialAd = ad;

                interstitialAd.OnAdFullScreenContentClosed += () =>
                {
                    interstitialClosed = true;
                    interstitialAd.Destroy();
                    interstitialAd = null;
                    LoadInterstitial();
                };

                interstitialAd.OnAdFullScreenContentFailed += _ =>
                {
                    interstitialClosed = true;
                    interstitialAd.Destroy();
                    interstitialAd = null;
                    LoadInterstitial();
                };
            }
        );
    }

    public void ShowInterstitial(Action onClosed)
    {
        if (!IsOnline)
        {
            onClosed?.Invoke();
            return;
        }

        if (!IsInterstitialReady || !CanShowInterstitial)
        {
            onClosed?.Invoke();
            TryLoadAllAds();
            return;
        }

        lastInterstitialTime = Time.unscaledTime;
        interstitialClosed = false;

        interstitialAd.Show();
        StartCoroutine(WaitInterstitial(onClosed));
    }

    IEnumerator WaitInterstitial(Action onClosed)
    {
        while (!interstitialClosed)
            yield return null;

        yield return null;
        onClosed?.Invoke();
    }

    // =====================================================
    // ===================== REWARDED ======================
    void LoadRewarded()
    {
        if (isRewardedLoading || !IsOnline) return;

        isRewardedLoading = true;

        RewardedAd.Load(
            TEST_REWARDED_ID,
            new AdRequest(),
            (ad, error) =>
            {
                isRewardedLoading = false;

                if (error != null || ad == null)
                {
                    Debug.LogWarning("❌ Rewarded load failed");
                    return;
                }

                rewardedAd = ad;

                rewardedAd.OnAdFullScreenContentClosed += () =>
                {
                    rewardedAd.Destroy();
                    rewardedAd = null;
                    LoadRewarded();
                };

                rewardedAd.OnAdFullScreenContentFailed += _ =>
                {
                    rewardedAd.Destroy();
                    rewardedAd = null;
                    LoadRewarded();
                };
            }
        );
    }

    public void ShowRewarded(Action onReward)
    {
        if (!IsOnline || !CanShowRewarded || !IsRewardedReady)
        {
            TryLoadAllAds();
            return;
        }

        lastRewardedTime = Time.unscaledTime;

        rewardedAd.Show(_ =>
        {
            onReward?.Invoke();
        });
    }

    void OnDestroy()
    {
        bannerView?.Destroy();
        interstitialAd?.Destroy();
        rewardedAd?.Destroy();
    }
}
