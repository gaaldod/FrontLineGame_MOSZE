using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class SettingsMenuManager : MonoBehaviour
{
    public Slider volume_master, volume_music, volume_effects;
    public Toggle toggle_fullscreen;
    public AudioMixer audioMixer;

    void Start()
    {
        // Sync the toggle with the current fullscreen state
        if (toggle_fullscreen != null)
        {
            toggle_fullscreen.isOn = Screen.fullScreen;
        }
        else 
        {             
            Debug.LogWarning("Fullscreen toggle is not assigned in the inspector.");
        }
    }

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

    public void SetFullScreen(bool fullScreenValue)
    {
        Screen.fullScreen = fullScreenValue;
        if (!fullScreenValue)
        {
            Resolution resolution = Screen.currentResolution;
            Screen.SetResolution(resolution.width, resolution.height, fullScreenValue);
        }

    }

    private System.Collections.IEnumerator RefreshFullscreen()
    {
        yield return new WaitForEndOfFrame();
        // Sometimes needed to force the change
        Screen.fullScreen = !Screen.fullScreen;
        yield return new WaitForEndOfFrame();
        Screen.fullScreen = !Screen.fullScreen;
    }
}