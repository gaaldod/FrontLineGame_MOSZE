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
    public GameObject saveBrowser;
    public GameObject BackGround;

    [Header("Save-related Buttons")]
    public Button continueButton;
    public Button savesButton;

    [Header("Save Settings")]
    public string saveFolderName = "saves";
    private string saveFolderPath;
    
    [Header("Skip Intro Settings")]
    [Tooltip("The key to press to skip the intro")]
    public KeyCode skipKey = KeyCode.Space;

    // Stores original button colors to restore when re-enabling
    private readonly Dictionary<Button, ColorBlock> originalButtonColors = new Dictionary<Button, ColorBlock>();

    // PlayerPrefs key used to remember that the intro has already been shown
    private const string IntroSeenKey = "FrontlineGame_IntroSeen_v1";

    // Runtime-only flag: if true, never show intro again until the app restarts (survives scene reloads)
    private static bool s_skipIntroForRestOfRuntime = false;

    public static void SkipIntroForRestOfRuntime()
    {
        s_skipIntroForRestOfRuntime = true;
    }

    // References for stopping playback reliably
    private PlayableDirector playableDirector;
    private bool introStarted = false;
    
    // Track maximum time reached to detect when intro has played enough
    private double maxTimeReached = 0.0;
    
    // Flag to prevent multiple skip calls
    private bool skipInProgress = false;
    
    // Flag to track if we're currently showing the intro (for input detection)
    private bool isShowingIntro = false;


    void Start()
    {
        Debug.Log($"MainMenu.Start() called. gameObject={gameObject.name}, active={gameObject.activeInHierarchy}, enabled={enabled}");
        // CRITICAL: Ensure this GameObject stays active so Update() can run and detect key presses
        if (!gameObject.activeInHierarchy)
        {
            gameObject.SetActive(true);
        }
        
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

        // Initialize save system first so we can decide whether to show the intro
        InitializeSaveSystem();

        // If we already entered gameplay in this runtime (or intro already ran), never replay intro.
        if (s_skipIntroForRestOfRuntime)
        {
            if (introScenePanel != null)
            {
                introScenePanel.SetActive(false);
                Canvas introCanvas = introScenePanel.GetComponent<Canvas>();
                if (introCanvas != null) introCanvas.enabled = false;
            }
            ShowMainMenu();
            CheckForSaveFiles();
            return;
        }

        bool saveFilesExist = DoSaveFilesExist();
        bool introSeen = PlayerPrefs.HasKey(IntroSeenKey);

        // Show intro only if there are no save files AND the intro has not been shown before
        if (!saveFilesExist && !introSeen)
        {
            if (!introStarted)
            {
                introStarted = true;
                StartIntro();
            }
        }
        else
        {
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
                }
            }
            ShowMainMenu();
            CheckForSaveFiles();
        }
    }
    
    void Update()
    {
        // Ensure GameObject stays active - critical for key detection
        if (!gameObject.activeInHierarchy)
        {
            gameObject.SetActive(true);
            return; // Skip this frame, will check next frame
        }
        
        // Try to find introScenePanel if it's not assigned
        if (introScenePanel == null)
        {
            GameObject foundPanel = GameObject.Find("introScene");
            if (foundPanel == null) foundPanel = GameObject.Find("IntroScene");
            if (foundPanel != null)
            {
                introScenePanel = foundPanel;
            }
        }
        
        // Ensure playableDirector is set
        if (playableDirector == null && introScenePanel != null)
        {
            playableDirector = introScenePanel.GetComponent<PlayableDirector>();
            if (playableDirector == null)
            {
                playableDirector = introScenePanel.GetComponentInChildren<PlayableDirector>();
            }
        }
        
        // Check if intro panel is active
        bool introPanelActive = introScenePanel != null && introScenePanel.activeInHierarchy;
        
        // Update isShowingIntro flag based on panel state
        // IMPORTANT: Keep isShowingIntro true if panel is active OR if we just set it in StartIntro()
        // This ensures input detection works even if PlayableDirector stops immediately
        if (introPanelActive)
        {
            isShowingIntro = true;
        }
        else if (!introPanelActive && isShowingIntro)
        {
            // Only clear flag if panel has been inactive for a moment (handled by DelayedHideIntro)
            // Don't clear immediately to allow input detection
        }
        
        // Track maximum time reached while timeline is playing
        if (playableDirector != null && playableDirector.enabled && playableDirector.state == PlayState.Playing)
        {
            double currentTime = playableDirector.time;
            if (currentTime > maxTimeReached)
            {
                maxTimeReached = currentTime;
            }
        }
        
        // Check for skip key press - check BOTH isShowingIntro flag AND panel active state
        // This ensures we catch input even if timing is off
        if ((isShowingIntro || introPanelActive) && !skipInProgress)
        {
            if (Input.GetKeyDown(skipKey))
            {
                skipInProgress = true;
                SkipIntro();
            }
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
            savesButton.interactable = true;
            SetButtonVisualState(savesButton, true);
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
        // From now on, never replay the intro in this app runtime (even if we return to menu)
        s_skipIntroForRestOfRuntime = true;

        // Create an initial save (will create the folder if missing).
        // Use sensible initial values; adjust round/maxRounds as needed.
        bool ok = SaveManager.SaveWorldState(round: 0, maxRounds: 10);
        Debug.Log($"MainMenu.PlayGame: SaveManager.SaveWorldState returned: {ok}. persistentDataPath: {Application.persistentDataPath}");

        SceneManager.LoadScene("WorldMapScene");
        //SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    // Called when Continue button is pressed
    void ContinueGame()
    {
        // From now on, never replay the intro in this app runtime (even if we return to menu)
        s_skipIntroForRestOfRuntime = true;

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
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (saveBrowser != null) saveBrowser.SetActive(true);
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
        if (saveBrowser != null) saveBrowser.SetActive(false);
    }

    public void ShowIntroScene()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (introScenePanel != null) introScenePanel.SetActive(true);
        if (saveBrowser != null) saveBrowser.SetActive(false);
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

        if (saveBrowser != null) saveBrowser.SetActive(false);

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
                // Check if Canvas is on the same GameObject as this script
                if (parentCanvas.gameObject == gameObject)
                {
                    // This is the MainMenu GameObject - ensure it's active and Canvas is enabled
                    if (!gameObject.activeInHierarchy)
                    {
                        gameObject.SetActive(true);
                    }
                    parentCanvas.enabled = true; // Re-enable Canvas component
                    parentCanvas.sortingOrder = 10;
                    Debug.Log($"ShowMainMenu() - Re-enabled Canvas component on MainMenu GameObject: Canvas enabled: {parentCanvas.enabled}, SortOrder: {parentCanvas.sortingOrder}");
                }
                else
                {
                    // Different GameObject - safe to activate the whole thing
                    parentCanvas.gameObject.SetActive(true);
                    parentCanvas.enabled = true; // Also explicitly enable the Canvas component
                    // Set sort order to ensure it renders on top
                    parentCanvas.sortingOrder = 10;
                    Debug.Log($"ShowMainMenu() - Enabled parent Canvas: {parentCanvas.gameObject.name}, Canvas enabled: {parentCanvas.enabled}, SortOrder: {parentCanvas.sortingOrder}");
                }
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

    private void StartIntro()
    {
        maxTimeReached = 0.0;

        // Once intro starts, don't allow it to replay again during this runtime (e.g., after returning from gameplay)
        s_skipIntroForRestOfRuntime = true;

        PlayerPrefs.SetInt(IntroSeenKey, 1);
        PlayerPrefs.Save();

        if (mainMenuPanel != null)
        {
            // mainMenuPanel IS the GameObject with this script (MainMenu)
            // We cannot disable it or the script will stop running!
            // Instead, disable only its child UI elements (buttons, etc.)
            
            // Disable all child UI elements (buttons, text, etc.) but keep MainMenu GameObject active
            foreach (Transform child in mainMenuPanel.transform)
            {
                child.gameObject.SetActive(false);
            }
            
            // CRITICAL: Never disable MenuUI (parent Canvas GameObject) or MainMenu (this GameObject)
            // Get the parent Canvas to ensure we don't disable it
            Canvas menuUICanvas = mainMenuPanel.GetComponentInParent<Canvas>();
            if (menuUICanvas != null && menuUICanvas.gameObject != mainMenuPanel)
            {
                // This is MenuUI - ensure it stays active!
                if (!menuUICanvas.gameObject.activeInHierarchy)
                {
                    menuUICanvas.gameObject.SetActive(true);
                }
            }
        }
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
        if (saveBrowser != null)
        {
            saveBrowser.SetActive(false);
        }

        if (introScenePanel == null)
        {
            GameObject foundPanel = GameObject.Find("introScene");
            if (foundPanel == null)
            {
                foundPanel = GameObject.Find("IntroScene");
            }
            
            if (foundPanel != null)
            {
                introScenePanel = foundPanel;
            }
            else
            {
                Debug.LogError("StartIntro() - introScenePanel is NULL and could not find 'introScene' GameObject! Please assign it in the Inspector.");
                return;
            }
        }
        
        // Enable Canvas FIRST before activating the panel
        Canvas introCanvas = introScenePanel.GetComponent<Canvas>();
        if (introCanvas != null)
        {
            introCanvas.enabled = true; // Enable Canvas BEFORE activating GameObject
        }
        
        Canvas parentCanvas = introScenePanel.GetComponentInParent<Canvas>();
        if (parentCanvas != null && parentCanvas.gameObject != introScenePanel)
        {
            parentCanvas.gameObject.SetActive(true);
            parentCanvas.enabled = true;
        }
        
        // Now activate the panel
        introScenePanel.SetActive(true);
        
        // Verify Canvas is still enabled after activation
        if (introCanvas != null)
        {
            if (!introCanvas.enabled)
            {
                introCanvas.enabled = true;
            }
        }
        
        // CRITICAL: Ensure this GameObject stays active so Update() can run and detect key presses
        if (!gameObject.activeInHierarchy)
        {
            gameObject.SetActive(true);
        }
        
        isShowingIntro = true;
        skipInProgress = false;
        
        // Disable background_gray when intro starts
        SetBackgroundGrayActive(false);

        playableDirector = null;

        playableDirector = introScenePanel != null ? introScenePanel.GetComponent<PlayableDirector>() : null;
        if (playableDirector == null && introScenePanel != null)
        {
            playableDirector = introScenePanel.GetComponentInChildren<PlayableDirector>();
        }
        
        if (playableDirector != null)
        {
            playableDirector.Stop();
            
            playableDirector.extrapolationMode = DirectorWrapMode.None;
            
            var wrapModeProperty = typeof(PlayableDirector).GetProperty("wrapMode");
            if (wrapModeProperty != null)
            {
                wrapModeProperty.SetValue(playableDirector, DirectorWrapMode.None);
            }
            
            playableDirector.playOnAwake = false;
            
            playableDirector.stopped += OnPlayableDirectorStopped;
            
            playableDirector.time = 0;
            playableDirector.Play();
            
            playableDirector.extrapolationMode = DirectorWrapMode.None;
            if (wrapModeProperty != null)
            {
                wrapModeProperty.SetValue(playableDirector, DirectorWrapMode.None);
            }
            
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
                    }
                }
            }
        }
    }

    public void OnIntroAnimationEnd()
    {
    }

    private GameObject FindBackgroundGray()
    {
        // Try using the BackGround field if assigned
        if (BackGround != null)
        {
            return BackGround;
        }
        
        // Try to find by name
        GameObject bg = GameObject.Find("background_gray");
        if (bg == null)
        {
            bg = GameObject.Find("Background_gray");
        }
        if (bg == null)
        {
            bg = GameObject.Find("BackgroundGray");
        }
        
        return bg;
    }

    private void SetBackgroundGrayActive(bool active)
    {
        GameObject bg = FindBackgroundGray();
        if (bg != null)
        {
            bg.SetActive(active);
        }
    }

    private void SkipIntro()
    {
        isShowingIntro = false;

        // Once skipped, keep it skipped for the rest of this runtime.
        s_skipIntroForRestOfRuntime = true;
        
        if (playableDirector != null)
        {
            playableDirector.stopped -= OnPlayableDirectorStopped;
            playableDirector.Stop();
            playableDirector.enabled = false;
        }
        
        if (introScenePanel != null)
        {
            introScenePanel.SetActive(false);
            
            Canvas introCanvas = introScenePanel.GetComponent<Canvas>();
            if (introCanvas != null)
            {
                introCanvas.enabled = false;
            }
            
            Canvas parentCanvas = introScenePanel.GetComponentInParent<Canvas>();
            if (parentCanvas != null && parentCanvas.gameObject != introScenePanel)
            {
                parentCanvas.gameObject.SetActive(false);
            }
        }
        
        // Re-enable background_gray when skipping intro
        SetBackgroundGrayActive(true);
        
        ShowMainMenu();
        CheckForSaveFiles();
    }

    private void OnPlayableDirectorStopped(PlayableDirector pd)
    {
        if (pd != null)
        {
            pd.stopped -= OnPlayableDirectorStopped;
            pd.Stop();
            pd.enabled = false;
            
            var wrapModeProperty = typeof(PlayableDirector).GetProperty("wrapMode");
            if (wrapModeProperty != null)
            {
                wrapModeProperty.SetValue(pd, DirectorWrapMode.None);
            }
        }
        
        // CRITICAL: Ensure GameObject is active so Update() can run and detect keys
        if (!gameObject.activeInHierarchy)
        {
            gameObject.SetActive(true);
        }
        
        // Check for skip key press immediately when timeline stops (in case Update() hasn't run yet)
        if ((isShowingIntro || (introScenePanel != null && introScenePanel.activeInHierarchy)) && !skipInProgress)
        {
            if (Input.GetKeyDown(skipKey))
            {
                skipInProgress = true;
                SkipIntro();
                return; // Exit early, skip handled
            }
        }
        
        bool completedNormally = pd != null && (maxTimeReached >= pd.duration - 0.1f || maxTimeReached >= 5.0);
        
        // Always delay hiding to give Update() a chance to detect skip key presses
        // This is especially important if the timeline stops immediately
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(DelayedHideIntro());
        }
        else
        {
            // Force GameObject active - this is critical for Update() to run
            gameObject.SetActive(true);
            StartCoroutine(DelayedHideIntro());
        }
    }
    
    private IEnumerator DelayedHideIntro()
    {
        // Wait several frames to give Update() multiple chances to detect key presses
        // This is especially important if the timeline stops immediately
        for (int i = 0; i < 5; i++)
        {
            yield return null; // Wait one frame
            // Check if skip was pressed during the delay
            if (skipInProgress)
            {
                yield break; // Exit coroutine, skip already handled
            }
        }
        
        if (!skipInProgress && isShowingIntro)
        {
            HideIntroAndShowMainMenu();
        }
    }
    
    private void HideIntroAndShowMainMenu()
    {
        isShowingIntro = false;

        // Once completed, keep it skipped for the rest of this runtime.
        s_skipIntroForRestOfRuntime = true;
        
        if (introScenePanel != null)
        {
            introScenePanel.SetActive(false);
            
            Canvas introCanvas = introScenePanel.GetComponent<Canvas>();
            if (introCanvas != null)
            {
                introCanvas.enabled = false;
            }
            
            Canvas parentCanvas = introScenePanel.GetComponentInParent<Canvas>();
            if (parentCanvas != null && parentCanvas.gameObject != introScenePanel)
            {
                parentCanvas.gameObject.SetActive(false);
            }
        }
        
        // Re-enable background_gray when intro ends
        SetBackgroundGrayActive(true);
        
        ShowMainMenu();
        CheckForSaveFiles();
    }
}