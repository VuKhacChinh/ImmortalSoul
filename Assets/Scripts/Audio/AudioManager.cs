using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;

    [Header("Audio Mixer")]
    public AudioMixer audioMixer;

    const string MUSIC_VOL = "MusicVolume";
    const string SFX_VOL = "SFXVolume";

    const float MUTE_DB = -80f;
    const float NORMAL_DB = 0f;

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

        LoadSettings();
    }

    // ================= MUSIC =================
    public void PlayMusic(AudioClip clip)
    {
        if (clip == null) return;

        if (musicSource.clip == clip && musicSource.isPlaying)
            return;

        musicSource.clip = clip;
        musicSource.loop = true;
        musicSource.Play();
    }

    public void StopMusic()
    {
        musicSource.Stop();
    }

    // ================= SFX =================
    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;
        sfxSource.PlayOneShot(clip);
    }

    // ================= GLOBAL TOGGLE =================
    public void ToggleVolume()
    {
        // Lấy trạng thái hiện tại từ Music
        audioMixer.GetFloat(MUSIC_VOL, out float currentMusicVol);
        bool isMuted = currentMusicVol <= -79f;

        float targetVol = isMuted ? NORMAL_DB : MUTE_DB;

        // Set cho cả Music + SFX
        audioMixer.SetFloat(MUSIC_VOL, targetVol);
        audioMixer.SetFloat(SFX_VOL, targetVol);

        // Save
        PlayerPrefs.SetInt("Music", isMuted ? 1 : 0);
        PlayerPrefs.SetInt("SFX", isMuted ? 1 : 0);
        PlayerPrefs.Save();
    }

    // ================= SETTINGS =================
    void LoadSettings()
    {
        bool musicOn = PlayerPrefs.GetInt("Music", 1) == 1;
        bool sfxOn = PlayerPrefs.GetInt("SFX", 1) == 1;

        audioMixer.SetFloat(MUSIC_VOL, musicOn ? NORMAL_DB : MUTE_DB);
        audioMixer.SetFloat(SFX_VOL, sfxOn ? NORMAL_DB : MUTE_DB);
    }

    // (Optional) cho UI icon hỏi trạng thái
    public bool IsSoundOn()
    {
        audioMixer.GetFloat(MUSIC_VOL, out float vol);
        return vol > -79f;
    }
}
