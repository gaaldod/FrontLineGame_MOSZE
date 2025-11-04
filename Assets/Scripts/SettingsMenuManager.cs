using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class SettingsMenuManager : MonoBehaviour
{
    public Slider volume_master, volume_music, volume_effects;
    public Toggle toggle_fullscreen;
    public AudioMixer audioMixer;

    public void ChangeMasterVolume()
    {
        audioMixer.SetFloat("volume_master", volume_master.value);
    }
    public void ChangeMusicVolume()
    {
        audioMixer.SetFloat("volume_music", volume_music.value);
    }
    public void ChangeEffectsVolume()
    {
        audioMixer.SetFloat("volume_effects", volume_effects.value);
    }
    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
    }
}
