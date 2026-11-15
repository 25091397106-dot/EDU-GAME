using UnityEngine;

public class MusicToggle : MonoBehaviour
{
    public AudioSource musicSource;
    private bool musicOn = true;

    public void ToggleMusic()
    {
        musicOn = !musicOn;
        musicSource.mute = !musicOn;
        Debug.Log(musicOn ? "Musik ON" : "Musik OFF");
    }
}