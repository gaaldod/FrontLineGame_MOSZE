using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    [Header("UI Referenciák")]
    public GameObject pauseMenuPanel; // A panel
    public Button resumeButton;       // A folytatás gomb
    public Button saveAndExitButton;  // A mentés és kilépés gomb

    private bool isPaused = false;

    void Start()
    {
        // Induláskor biztosítjuk, hogy a menü rejtve legyen
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);

        // Gombok bekötése
        if (resumeButton != null)
            resumeButton.onClick.AddListener(ResumeGame);

        if (saveAndExitButton != null)
            saveAndExitButton.onClick.AddListener(SaveAndExit);
    }

    void Update()
    {
        // Figyeljük az ESC-et VAGY a 'P' betût a teszteléshez
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log("Gomb lenyomva! Pause logika indul"); // Debug üzenet

            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    public void PauseGame()
    {
        isPaused = true;

        // UI megjelenítése
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(true);

        // Opcionális: Megállítjuk az idõt (hogy ne mozogjanak a katonák a háttérben)
        Time.timeScale = 0f;
        Debug.Log("Játék megállítva (Paused).");
    }

    public void ResumeGame()
    {
        isPaused = false;

        // UI elrejtése
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);

        // Idõ újraindítása
        Time.timeScale = 1f;
        Debug.Log("Játék folytatódik.");
    }

    void SaveAndExit()
    {
        Debug.Log("Mentés és Kilépés...");

        // 1. MENTÉS
        int currentRound = 0;
        int maxRounds = 10; // Default, ha nincs WM

        if (WorldManager.Instance != null)
        {
            currentRound = WorldManager.Instance.currentRound;
            maxRounds = WorldManager.MAX_ROUNDS;
        }

        // Elmentjük az állapotot
        SaveManager.SaveWorldState(currentRound, maxRounds);

        // 2. IDÕ VISSZAÁLLÍTÁSA (NAGYON FONTOS!)
        Time.timeScale = 1f;

        // 3. KILÉPÉS A FÕMENÜBE
        SceneManager.LoadScene("MainMenu");
    }
}