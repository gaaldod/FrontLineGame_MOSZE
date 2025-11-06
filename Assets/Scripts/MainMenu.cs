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

    [Header("Save-related Buttons")]
    public Button continueButton;
    public Button savesButton;

    [Header("Save Settings")]
    public string saveFolderName = "saves";
    private string saveFolderPath;
    
    // Stores original button colors to restore when re-enabling
    private readonly Dictionary<Button, ColorBlock> originalButtonColors = new Dictionary<Button, ColorBlock>();

    void Start()
    {
        // Ensure only main menu is visible at startup
        ShowMainMenu();

        // Initialize save system and check for saves
        InitializeSaveSystem();
        CheckForSaveFiles();
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
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void ShowSettings()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    public void ShowMainMenu()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
    }
}