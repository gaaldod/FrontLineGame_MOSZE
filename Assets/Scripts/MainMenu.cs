using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.IO;


public class MainMenu : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject mainMenuPanel;
    public GameObject settingsPanel;
    public GameObject introScenePanel;

    [Header("Save-related Buttons")]
    public Button continueButton;
    public Button savesButton;

    [Header("Save Settings")]
    public string saveFolderName = "saves";
    private string saveFolderPath;
    
    // Stores original button colors to restore when re-enabling
    private readonly Dictionary<Button, ColorBlock> originalButtonColors = new Dictionary<Button, ColorBlock>();

    // PlayerPrefs key used to remember that the intro has already been shown
    private const string IntroSeenKey = "FrontlineGame_IntroSeen_v1";

    // Control flag for animation event/end
    private bool introFinishedByEvent = false;


    void Start()
    {
        // Initialize save system first so we can decide whether to show the intro
        InitializeSaveSystem();

        bool saveFilesExist = DoSaveFilesExist();

        // Show intro only if there are no save files AND the intro has not been shown before
        if (!saveFilesExist && !PlayerPrefs.HasKey(IntroSeenKey))
        {
            // Play intro and after it finishes, show main menu and initialize save checks
            StartCoroutine(PlayIntroThenShowMain());
        }
        else
        {
            // Normal startup: show main menu immediately and check saves
            ShowMainMenu();
            CheckForSaveFiles();
        }
    }

    void InitializeSaveSystem()
    {
        // This will work in both Editor and built game
        saveFolderPath = Path.Combine(Application.persistentDataPath, saveFolderName);
    }

    void CheckForSaveFiles()
    {
        bool saveFilesExist = DoSaveFilesExist();

        // Enable/disable buttons based on save file existence
        if (continueButton != null)
        {
            continueButton.interactable = saveFilesExist;
            // Optional: Change color to grey when disabled
            SetButtonVisualState(continueButton, saveFilesExist);
        }

        if (savesButton != null)
        {
            savesButton.interactable = saveFilesExist;
            SetButtonVisualState(savesButton, saveFilesExist);
        }

        Debug.Log($"Save files exist: {saveFilesExist}");
    }

    bool DoSaveFilesExist()
    {
        // Check if save directory exists and has files
        if (!Directory.Exists(saveFolderPath))
            return false;

        // Get all files in the save directory (you can modify this filter later)
        string[] saveFiles = Directory.GetFiles(saveFolderPath, "*.save");
        return saveFiles.Length > 0;
    }

    void SetButtonVisualState(Button button, bool isActive)
    {
        if (!isActive)
        {
            // Save original colors once so we can restore when re-enabling
            if (!originalButtonColors.ContainsKey(button))
            {
                originalButtonColors[button] = button.colors;
            }

            // Make button appear greyed out
            var colors = button.colors;
            colors.normalColor = Color.gray;
            colors.disabledColor = Color.gray;
            button.colors = colors;
        }
        else
        {
            // Restore original colors if we saved them previously
            if (originalButtonColors.TryGetValue(button, out var originalColors))
            {
                button.colors = originalColors;
            }
            // If no original colors were saved, do not modify colors
        }
    }

    // Your existing methods remain the same
    public void PlayGame()
    {
        SceneManager.LoadScene("WorldMapScene");
        //SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    public void QuitGame()
    {
        Debug.Log("Kilépés a játékból...");
        Application.Quit();
    }

    public void ShowSettings()
    {
        if (introScenePanel != null) introScenePanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    public void ShowIntroScene() 
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (introScenePanel != null) introScenePanel.SetActive(true);
    }

    public void ShowMainMenu()
    {
        if (introScenePanel != null) introScenePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
    }

    // Coroutine that handles intro playback, waits for animation to finish, marks intro seen and shows main menu
    private IEnumerator PlayIntroThenShowMain()
    {
        introFinishedByEvent = false;

        // Ensure main menu/settings are hidden while intro plays
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        // Enable intro UI
        if (introScenePanel != null) introScenePanel.SetActive(true);

        // Try Animator first (Animator + runtime controller)
        float waitTime = 0f;
        Animator animator = introScenePanel != null ? introScenePanel.GetComponent<Animator>() : null;
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            // Determine a safe clip length (take the longest clip on the controller)
            foreach (var clip in animator.runtimeAnimatorController.animationClips)
            {
                if (clip != null && clip.length > waitTime) waitTime = clip.length;
            }
            // Play default state to ensure it starts (use layer 0)
            animator.Play(0, 0, 0f);
        }
        else
        {
            // Fallback to legacy Animation component
            Animation legacy = introScenePanel != null ? introScenePanel.GetComponent<Animation>() : null;
            if (legacy != null)
            {
                // Play default clip if assigned and get its length
                if (legacy.clip != null)
                {
                    waitTime = legacy.clip.length;
                    legacy.Play();
                }
                else
                {
                    // Try any clip on the Animation component
                    foreach (AnimationState state in legacy)
                    {
                        if (state != null && state.clip != null && state.clip.length > waitTime)
                            waitTime = state.clip.length;
                    }
                    if (waitTime > 0f) legacy.Play();
                }
            }
        }
        // If no animator/animation or no clip length detected, fallback to 3 seconds
        if (waitTime <= 0f) waitTime = 3f;

        // Wait until either the animation finished or an animation event calls OnIntroAnimationEnd()
        float elapsed = 0f;
        while (elapsed < waitTime && !introFinishedByEvent)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Mark intro as shown so it won't play again
        PlayerPrefs.SetInt(IntroSeenKey, 1);
        PlayerPrefs.Save();

        // Hide intro and show main menu, then update save-button states
        if (introScenePanel != null) introScenePanel.SetActive(false);
        ShowMainMenu();
        CheckForSaveFiles();
    }

    // Public method you can call from an Animation Event at the end of the clip to end intro immediately
    public void OnIntroAnimationEnd()
    {
        introFinishedByEvent = true;
    }
}