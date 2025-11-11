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
    
    [Header("Demo Settings")]
    // Set true to temporarily disable the saves button for demos
    public bool disableSavesButtonInDemo = true;

    // Stores original button colors to restore when re-enabling
    private readonly Dictionary<Button, ColorBlock> originalButtonColors = new Dictionary<Button, ColorBlock>();

    void Start()
    {
        Debug.Log($"MainMenu.Start() called. gameObject={gameObject.name}, active={gameObject.activeInHierarchy}, enabled={enabled}");
        // Ensure only main menu is visible at startup
        ShowMainMenu();

        // Initialize save system and check for saves
        InitializeSaveSystem();
        CheckForSaveFiles();

        // Wire up listeners for save-related buttons
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(ContinueGame);
        }

        if (savesButton != null)
        {
            savesButton.onClick.AddListener(OpenSavesBrowserUI);
        }

        // DEMO: temporarily disable saves button so it can't be used during presentation
        if (disableSavesButtonInDemo && savesButton != null)
        {
            savesButton.interactable = false;
            SetButtonVisualState(savesButton, false);
            savesButton.onClick.RemoveAllListeners();
            Debug.Log("MainMenu: savesButton disabled for demo (disableSavesButtonInDemo=true)");
        }
    }

    void InitializeSaveSystem()
    {
        saveFolderPath = Path.Combine(Application.persistentDataPath, saveFolderName);
        Debug.Log($"MainMenu.InitializeSaveSystem: saveFolderName='{saveFolderName}', saveFolderPath='{saveFolderPath}'");
    }

    void CheckForSaveFiles()
    {
        Debug.Log("MainMenu.CheckForSaveFiles() called");
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
            // Respect demo override: if demo flag set, keep saves button disabled
            bool allowSaves = !disableSavesButtonInDemo && saveFilesExist;
            savesButton.interactable = allowSaves;
            SetButtonVisualState(savesButton, allowSaves);
        }

        Debug.Log($"MainMenu.CheckForSaveFiles: Save files exist: {saveFilesExist}");
    }

    bool DoSaveFilesExist()
    {
        try
        {
            Debug.Log($"MainMenu.DoSaveFilesExist: checking save folder path = {saveFolderPath}");
            if (Directory.Exists(saveFolderPath))
            {
                string[] saveFiles = Directory.GetFiles(saveFolderPath, "*.json");
                Debug.Log($"MainMenu.DoSaveFilesExist: found {saveFiles.Length} files in save folder.");
                return saveFiles.Length > 0;
            }

            // Fallback: search the entire persistentDataPath for any save*.json (covers editor/build path mismatches)
            string root = Application.persistentDataPath;
            Debug.Log($"MainMenu.DoSaveFilesExist: save folder not found. Searching persistentDataPath = {root} for save*.json");
            string[] found = Directory.GetFiles(root, "save*.json", SearchOption.AllDirectories);
            Debug.Log($"MainMenu.DoSaveFilesExist: fallback search found {found.Length} save files.");
            return found.Length > 0;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"MainMenu.DoSaveFilesExist: EXCEPTION: {ex}");
            return false;
        }
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
        // Create an initial save (will create the folder if missing).
        // Use sensible initial values; adjust round/maxRounds as needed.
        bool ok = SaveManager.SaveWorldState(round: 0, maxRounds: 14);
        Debug.Log($"MainMenu.PlayGame: SaveManager.SaveWorldState returned: {ok}. persistentDataPath: {Application.persistentDataPath}");

        SceneManager.LoadScene("WorldMapScene");
        //SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    // Called when Continue button is pressed
    void ContinueGame()
    {
        bool loaded = SaveManager.LoadLatestSave();
        if (!loaded)
        {
            Debug.LogWarning("MainMenu.ContinueGame: no save found, starting new game.");
        }
        // Load the world scene regardless; WorldManager consumes PendingLoad on scene load.
        SceneManager.LoadScene("WorldMapScene");
    }

    // Called when Saves button is pressed - open in-game browser
    void OpenSavesBrowserUI()
    {
        SaveBrowser.Open();
    }

    public void QuitGame()
    {
        Debug.Log("Kilépés a játékból...");
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