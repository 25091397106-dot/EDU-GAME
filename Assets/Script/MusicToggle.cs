
using UnityEngine;

public class MusicToggle : MonoBehaviour
{
    public AudioSource musicSource;
    private bool musicOn = true;

    void Start()
    {
        if (musicSource == null)
            musicSource = GetComponent<AudioSource>();

        if (musicSource != null)
        {
            musicSource.volume = PlayerPrefs.GetFloat("VolumeMusik", 0.8f);
            musicSource.mute = !musicOn;
        }
    }

    public void ToggleMusic()
    {
        musicOn = !musicOn;

        if (musicSource != null)
            musicSource.mute = !musicOn;

        Debug.Log(musicOn ? "Musik ON" : "Musik OFF");
    }
}
