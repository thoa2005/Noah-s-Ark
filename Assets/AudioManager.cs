using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{

    public static AudioManager Instance;

    [Header("Music")]
    public AudioSource musicAudioSource;

    [Header("SFX")]
    public AudioSource sfxAudioSource;

    [Header("Ambient")]
    public AudioSource ambientAudioSource;

    [Header("Footstep")]
    public AudioSource footstepAudioSource;

    [Header("Music Clips")]
    public AudioClip lobbyMusic;
    public AudioClip gameplayMusic;

    [Header("SFX Clips")]
    public AudioClip buttonClick;
    public AudioClip punchSound;
    public AudioClip bomSound;
    public AudioClip lightningSound;

    [Header("Ambient Clips")]
    public AudioClip windSound;
    public AudioClip rainSound;

    [Header("Movement")]
    public AudioClip walkingSound;
    public AudioClip jumpSound;

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (SceneManager.GetActiveScene().name == "MainMenuScene")
        {
            musicAudioSource.clip = lobbyMusic;
        }
        else
        {
            musicAudioSource.clip = gameplayMusic;
            musicAudioSource.time = 15f;
        }

        musicAudioSource.loop = true;
        musicAudioSource.Play();
    }

    public void PlayButtonSound()
    {
        sfxAudioSource.PlayOneShot(buttonClick);
    }

    public void PlayPunchSound()
    {
        sfxAudioSource.PlayOneShot(punchSound);
    }

    public void PlayBomSound()
    {
        sfxAudioSource.PlayOneShot(bomSound);
    }

    public void PlayLightningSound()
    {
        sfxAudioSource.PlayOneShot(lightningSound);
    }

    public void StartWind()
    {
        ambientAudioSource.clip = windSound;
        ambientAudioSource.loop = true;
        ambientAudioSource.Play();
    }

    public void StartRain()
    {
        ambientAudioSource.clip = rainSound;
        ambientAudioSource.loop = true;
        ambientAudioSource.Play();
    }

    public void StopAmbient()
    {
        ambientAudioSource.Stop();
    }

    public void StartWalking()
{
    if (!footstepAudioSource.isPlaying)
    {
        footstepAudioSource.clip = walkingSound;
        footstepAudioSource.loop = true;
        footstepAudioSource.Play();
    }
}

public void StopWalking()
{
    footstepAudioSource.Stop();
}
   public void PlayJumpSound()
{
    Debug.Log("JUMP SOUND");
    sfxAudioSource.PlayOneShot(jumpSound);
}
}