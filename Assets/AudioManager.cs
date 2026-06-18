using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public AudioSource musicAudioSource;

    public AudioClip lobbyMusic;
    public AudioClip gameplayMusic;

    void Start()
    {
        if (SceneManager.GetActiveScene().name == "MainMenuScene")
        {
            musicAudioSource.clip = lobbyMusic;
        }
        else
        {
            musicAudioSource.clip = gameplayMusic;
        }

        musicAudioSource.loop = true;
        musicAudioSource.Play();

            musicAudioSource.clip = gameplayMusic;
    musicAudioSource.time = 15f;
    musicAudioSource.Play();
    }
}