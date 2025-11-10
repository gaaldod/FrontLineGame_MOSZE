using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.IO;
using UnityEngine.Playables;
using System.Reflection;

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

    // References for stopping playback reliably
    private PlayableDirector playableDirector;
    private bool introStarted = false;
    
    // Track maximum time reached to detect when intro has played enough
    private double maxTimeReached = 0.0;


    void Start()
    {
        Debug.Log("MainMenu.Start() called");
        // Initialize save system first so we can decide whether to show the intro
        InitializeSaveSystem();

        bool saveFilesExist = DoSaveFilesExist();
        bool introSeen = PlayerPrefs.HasKey(IntroSeenKey);
        Debug.Log($"MainMenu.Start() - Save files exist: {saveFilesExist}, Intro seen: {introSeen}");
        
        // TEMPORARY: Clear intro seen flag for testing (remove this line once intro works correctly)
        PlayerPrefs.DeleteKey(IntroSeenKey);
        introSeen = false;
        Debug.Log("MainMenu.Start() - Cleared IntroSeenKey for testing");

        // Show intro only if there are no save files AND the intro has not been shown before
        if (!saveFilesExist && !introSeen)
        {
            Debug.Log("MainMenu.Start() - Starting intro");
            if (!introStarted)
            {
                introStarted = true;
                StartIntro();
            }
            else
            {
                Debug.LogWarning("MainMenu.Start() - Intro already started, skipping");
            }
        }
        else
        {
            Debug.Log("MainMenu.Start() - Showing main menu (intro skipped)");
            // Normal startup: show main menu immediately and check saves
            // Make sure introScene is disabled if intro is skipped
            if (introScenePanel != null)
            {
                introScenePanel.SetActive(false);
                Canvas introCanvas = introScenePanel.GetComponent<Canvas>();
                if (introCanvas != null) introCanvas.enabled = false;
            }
            else
            {
                // Try to find and disable introScene by name
                GameObject introScene = GameObject.Find("introScene");
                if (introScene == null) introScene = GameObject.Find("IntroScene");
                if (introScene != null)
                {
                    introScene.SetActive(false);
                    Canvas introCanvas = introScene.GetComponent<Canvas>();
                    if (introCanvas != null) introCanvas.enabled = false;
                    Debug.Log("MainMenu.Start() - Found and disabled introScene by name");
                }
            }
            ShowMainMenu();
            CheckForSaveFiles();
        }
    }
    
    void Update()
    {
        // Track maximum time reached while timeline is playing
        if (playableDirector != null && playableDirector.enabled && playableDirector.state == PlayState.Playing)
        {
            double currentTime = playableDirector.time;
            if (currentTime > maxTimeReached)
            {
                maxTimeReached = currentTime;
            }
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
        Debug.Log($"ShowMainMenu() called - mainMenuPanel is {(mainMenuPanel == null ? "NULL" : "assigned")}");
        
        // First, make absolutely sure introScene is disabled
        if (introScenePanel != null) 
        {
            introScenePanel.SetActive(false);
            Debug.Log("ShowMainMenu() - Disabled introScenePanel");
            
            // Also disable the Canvas component if introScenePanel is the Canvas itself
            Canvas introCanvas = introScenePanel.GetComponent<Canvas>();
            if (introCanvas != null)
            {
                introCanvas.enabled = false;
                Debug.Log("ShowMainMenu() - Disabled introScene Canvas component");
            }
            
            // Also try to find and disable any Canvas parent
            Canvas parentIntroCanvas = introScenePanel.GetComponentInParent<Canvas>();
            if (parentIntroCanvas != null && parentIntroCanvas.gameObject != introScenePanel)
            {
                parentIntroCanvas.gameObject.SetActive(false);
                Debug.Log($"ShowMainMenu() - Disabled introScene parent Canvas: {parentIntroCanvas.gameObject.name}");
            }
        }
        
        if (settingsPanel != null) 
        {
            settingsPanel.SetActive(false);
            Debug.Log("ShowMainMenu() - Disabled settingsPanel");
        }
        
        if (mainMenuPanel != null) 
        {
            // Make sure the parent Canvas is enabled
            Canvas parentCanvas = mainMenuPanel.GetComponentInParent<Canvas>();
            if (parentCanvas != null)
            {
                parentCanvas.gameObject.SetActive(true);
                parentCanvas.enabled = true; // Also explicitly enable the Canvas component
                // Set sort order to ensure it renders on top
                parentCanvas.sortingOrder = 10;
                Debug.Log($"ShowMainMenu() - Enabled parent Canvas: {parentCanvas.gameObject.name}, Canvas enabled: {parentCanvas.enabled}, SortOrder: {parentCanvas.sortingOrder}");
            }
            else
            {
                // Try to find the main Canvas by name as fallback
                GameObject canvasObj = GameObject.Find("Canvas");
                if (canvasObj == null) canvasObj = GameObject.Find("MainMenuCanvas");
                if (canvasObj != null)
                {
                    canvasObj.SetActive(true);
                    Canvas canvas = canvasObj.GetComponent<Canvas>();
                    if (canvas != null) canvas.enabled = true;
                    Debug.Log($"ShowMainMenu() - Found and enabled Canvas by name: {canvasObj.name}");
                }
            }
            
            mainMenuPanel.SetActive(true);
            
            // Ensure all child UI elements are also enabled
            foreach (Transform child in mainMenuPanel.transform)
            {
                child.gameObject.SetActive(true);
            }
            
            // Ensure Canvas render mode is correct (should be Screen Space Overlay or Screen Space Camera)
            if (parentCanvas != null)
            {
                if (parentCanvas.renderMode == RenderMode.WorldSpace)
                {
                    Debug.LogWarning("ShowMainMenu() - Canvas is in World Space mode, this might cause visibility issues!");
                }
            }
            
            Debug.Log($"ShowMainMenu() - Enabled mainMenuPanel: {mainMenuPanel.name}, Active: {mainMenuPanel.activeSelf}, ActiveInHierarchy: {mainMenuPanel.activeInHierarchy}");
        }
        else
        {
            Debug.LogError("ShowMainMenu() - mainMenuPanel is NULL! Cannot show main menu.");
            // Try to find main menu panel by name as fallback
            GameObject foundPanel = GameObject.Find("MainMenu");
            if (foundPanel != null)
            {
                foundPanel.SetActive(true);
                Canvas parentCanvas = foundPanel.GetComponentInParent<Canvas>();
                if (parentCanvas != null)
                {
                    parentCanvas.gameObject.SetActive(true);
                    parentCanvas.enabled = true;
                }
                Debug.Log($"ShowMainMenu() - Found mainMenuPanel by name and enabled it: {foundPanel.name}");
            }
        }
    }

    // Simple void method to start the intro - no coroutine needed!
    private void StartIntro()
    {
        Debug.Log("StartIntro() called");
        maxTimeReached = 0.0;

        // Mark intro as seen immediately when it starts - user will either watch it or skip it
        PlayerPrefs.SetInt(IntroSeenKey, 1);
        PlayerPrefs.Save();
        Debug.Log("StartIntro() - Marked intro as seen (user is watching it now)");

        // Ensure main menu/settings are hidden while intro plays
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        // Enable intro UI - try to find it if not assigned
        if (introScenePanel == null)
        {
            // Try to find it by name - it's called "introScene"
            GameObject foundPanel = GameObject.Find("introScene");
            if (foundPanel == null)
            {
                foundPanel = GameObject.Find("IntroScene");
            }
            
            if (foundPanel != null)
            {
                introScenePanel = foundPanel;
                Debug.Log($"StartIntro() - Found introScenePanel by name: {foundPanel.name}");
            }
            else
            {
                Debug.LogError("StartIntro() - introScenePanel is NULL and could not find 'introScene' GameObject! Please assign it in the Inspector.");
                return;
            }
        }
        
        introScenePanel.SetActive(true);
        Debug.Log($"StartIntro() - introScenePanel activated: {introScenePanel.name}");

        // Reset reference
        playableDirector = null;

        // Try PlayableDirector (Timeline) first - check on introScenePanel and its children
        playableDirector = introScenePanel != null ? introScenePanel.GetComponent<PlayableDirector>() : null;
        if (playableDirector == null && introScenePanel != null)
        {
            playableDirector = introScenePanel.GetComponentInChildren<PlayableDirector>();
            Debug.Log($"StartIntro() - PlayableDirector found in children: {playableDirector != null}");
        }
        
        if (playableDirector != null)
        {
            Debug.Log($"StartIntro() - Found PlayableDirector on: {playableDirector.gameObject.name}");
            
            // CRITICAL: Stop any existing playback first
            playableDirector.Stop();
            
            // CRITICAL: Force wrap mode to None using multiple methods
            // Method 1: Set extrapolationMode (newer Unity API)
            playableDirector.extrapolationMode = DirectorWrapMode.None;
            
            // Method 2: Try to set wrapMode property via reflection (older Unity API)
            var wrapModeProperty = typeof(PlayableDirector).GetProperty("wrapMode");
            if (wrapModeProperty != null)
            {
                wrapModeProperty.SetValue(playableDirector, DirectorWrapMode.None);
                Debug.Log("StartIntro() - Set wrapMode to None via reflection");
            }
            
            // Method 3: Try to set it via the playableAsset if possible
            // The playableAsset itself might have loop settings, but we can't easily change those
            
            playableDirector.playOnAwake = false;
            
            Debug.Log($"StartIntro() - After setting wrap mode - ExtrapolationMode: {playableDirector.extrapolationMode}, WrapMode property exists: {wrapModeProperty != null}");
            
            // Subscribe to stopped so we reliably know when the timeline ends
            playableDirector.stopped += OnPlayableDirectorStopped;
            double directorDuration = playableDirector.duration;
            Debug.Log($"StartIntro() - PlayableDirector duration: {directorDuration:F2}s, Extrapolation: {playableDirector.extrapolationMode}, PlayOnAwake: {playableDirector.playOnAwake}");
            
            // Reset time to 0 before playing
            playableDirector.time = 0;
            playableDirector.Play();
            Debug.Log($"StartIntro() - PlayableDirector.Play() called, state: {playableDirector.state}, time: {playableDirector.time:F2}");
            
            // CRITICAL: Set wrap mode again AFTER playing, as some Unity versions reset it
            playableDirector.extrapolationMode = DirectorWrapMode.None;
            if (wrapModeProperty != null)
            {
                wrapModeProperty.SetValue(playableDirector, DirectorWrapMode.None);
            }
            Debug.Log($"StartIntro() - Set wrap mode again after Play()");
            
            // Also try to disable looping on individual clips if possible
            // Note: This might not work for all Unity versions, but worth trying
            if (playableDirector.playableAsset != null)
            {
                var playableAssetType = playableDirector.playableAsset.GetType();
                var tracksProperty = playableAssetType.GetProperty("tracks");
                if (tracksProperty != null)
                {
                    var tracks = tracksProperty.GetValue(playableDirector.playableAsset) as System.Collections.IEnumerable;
                    if (tracks != null)
                    {
                        foreach (var track in tracks)
                        {
                            var clipsProperty = track.GetType().GetProperty("clips");
                            if (clipsProperty != null)
                            {
                                var clips = clipsProperty.GetValue(track) as System.Collections.IEnumerable;
                                if (clips != null)
                                {
                                    foreach (var clip in clips)
                                    {
                                        var assetProperty = clip.GetType().GetProperty("asset");
                                        if (assetProperty != null)
                                        {
                                            var asset = assetProperty.GetValue(clip);
                                            var loopProperty = asset.GetType().GetProperty("loop");
                                            if (loopProperty != null && loopProperty.PropertyType == typeof(bool))
                                            {
                                                loopProperty.SetValue(asset, false);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        Debug.Log("StartIntro() - Attempted to disable looping on animation clips");
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning("StartIntro() - No PlayableDirector found on introScenePanel or its children!");
        }
    }

    // Public method you can call from an Animation Event at the end of the clip to end intro immediately
    public void OnIntroAnimationEnd()
    {
        Debug.Log("OnIntroAnimationEnd() called by Animation Event.");

    }

    // PlayableDirector stopped callback
    private void OnPlayableDirectorStopped(PlayableDirector pd)
    {
        Debug.Log($"PlayableDirector stopped event received. Time: {pd.time:F2}, MaxTimeReached: {maxTimeReached:F2}, Duration: {pd.duration:F2}, State: {pd.state}");
        
        // Since we mark intro as seen when it starts, we should hide it and show main menu whenever it stops
        // This handles both normal completion and early stops/restarts
        bool shouldShowMainMenu = true;
        
        // Only check if we've seen enough if maxTimeReached is still 0 (timeline stopped immediately)
        // Otherwise, if we've seen at least 5 seconds or reached duration, show main menu
        if (maxTimeReached < 5.0 && pd.time < pd.duration - 0.1f)
        {
            Debug.Log($"OnPlayableDirectorStopped() - Timeline stopped very early (time: {pd.time:F2}, maxTime: {maxTimeReached:F2}). Timeline might restart, but we'll hide intro anyway since it's marked as seen.");
        }
        
        if (shouldShowMainMenu)
        {
            Debug.Log("OnPlayableDirectorStopped() - Hiding intro and showing main menu");
            
            // CRITICAL: Immediately hide intro and show main menu, regardless of timeline state
            if (introScenePanel != null)
            {
                introScenePanel.SetActive(false);
                Debug.Log("OnPlayableDirectorStopped() - Disabled introScenePanel");
                
                // Also disable the Canvas component if introScenePanel is the Canvas itself
                Canvas introCanvas = introScenePanel.GetComponent<Canvas>();
                if (introCanvas != null)
                {
                    introCanvas.enabled = false;
                    Debug.Log("OnPlayableDirectorStopped() - Disabled introScene Canvas component");
                }
                
                // Also try to find and disable any Canvas parent
                Canvas parentCanvas = introScenePanel.GetComponentInParent<Canvas>();
                if (parentCanvas != null && parentCanvas.gameObject != introScenePanel)
                {
                    parentCanvas.gameObject.SetActive(false);
                    Debug.Log($"OnPlayableDirectorStopped() - Disabled parent Canvas: {parentCanvas.gameObject.name}");
                }
            }
            
            ShowMainMenu();
            CheckForSaveFiles();
            
            // Intro was already marked as seen when it started, so no need to mark it again
            Debug.Log("OnPlayableDirectorStopped() - Intro finished, showing main menu");
        }
        
        // CRITICAL: Immediately stop, disable, and unsubscribe to prevent ANY restart
        if (pd != null)
        {
            // Unsubscribe first to prevent recursive calls
            pd.stopped -= OnPlayableDirectorStopped;
            
            // Stop the director
            pd.Stop();
            
            // Disable the component entirely
            pd.enabled = false;
            
            // Try to set wrap mode to None again using reflection
            var wrapModeProperty = typeof(PlayableDirector).GetProperty("wrapMode");
            if (wrapModeProperty != null)
            {
                wrapModeProperty.SetValue(pd, DirectorWrapMode.None);
            }
            
            Debug.Log($"PlayableDirector disabled and stopped. Enabled: {pd.enabled}, State: {pd.state}");
        }
    }
}