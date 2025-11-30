using UnityEngine;
using UnityEngine.UI;
using System.IO;
using UnityEngine.SceneManagement;
using TMPro;

public class SaveBrowser : MonoBehaviour
{
    [Header("UI Referenciák (Húzd be õket!)")]
    public RectTransform contentContainer; // A Scroll View -> Content objektuma
    public GameObject saveItemPrefab;  // A Gomb prefab, amit a Projectbõl húzol be
    public ScrollRect scrollView;

    [Header("Funkció Gombok (Húzd be a Panelrõl!)")]
    public Button openFolderButton;    // A mappa megnyitása gomb
    public Button refreshButton;       // A frissítés gomb

    private void OnEnable()
    {
        Refresh();
    }
    public void Open()
    {
        gameObject.SetActive(true);
    }

    // Ezt hívja a Close gomb
    public void Close()
    {
        gameObject.SetActive(false);
    }

    void Start()
    {
        // Bekötjük a gombokat automatikusan, ha be vannak húzva az Inspectorban
        if (openFolderButton != null)
            openFolderButton.onClick.AddListener(OpenFolder);

        if (refreshButton != null)
            refreshButton.onClick.AddListener(Refresh);
    }

    // A mappa megnyitása
    void OpenFolder()
    {
        SaveManager.OpenSavesFolder();
    }

    // A lista frissítése (ez a lényeg)
    public void Refresh()
    {
        // 1. Töröljük a régi listát (hogy ne duplázódjon)
        foreach (Transform child in contentContainer)
        {
            Destroy(child.gameObject);
        }

        // 2. Lekérjük a fájlokat a SaveManager-tõl
        string[] files = SaveManager.GetSaveFiles();

        if (files == null || files.Length == 0)
        {
            Debug.Log("Nincsenek mentések.");
            return;
        }

        // 3. Legyártjuk a gombokat a Prefab alapján
        foreach (string path in files)
        {
            // Létrehozunk egy új gombot a Content alatt
            GameObject newButton = Instantiate(saveItemPrefab, contentContainer);

            // Adatok lekérése
            var fi = new FileInfo(path);

            // Szöveg beállítása (Feltételezzük, hogy van Text komponens a gombon)
            TMP_Text btnText = newButton.GetComponentInChildren<TMP_Text>();
            if (btnText != null)
            {
                // Formátum: Fájlnév (sortörés) Dátum
                btnText.text = $"{fi.Name}</size>";
            }

            // Kattintás esemény bekötése (Betöltés)
            string capturedPath = path; // Fontos a lambda miatt
            newButton.GetComponent<Button>().onClick.AddListener(() => LoadSave(capturedPath));
        }
        ResetScrollPosition();
    }
    void ResetScrollPosition()
    {
        if (scrollView != null)
        {
            // Ez a parancs kényszeríti a Unity-t, hogy AZONNAL számolja ki a lista új magasságát
            // Ha ez nincs itt, a görgetés "régi" adatokkal dolgozna és pontatlan lenne.
            Canvas.ForceUpdateCanvases();

            // 1 = Teteje, 0 = Alja
            scrollView.verticalNormalizedPosition = 1f;
        }
    }

    void LoadSave(string path)
    {
        Debug.Log($"Betöltés indítása: {path}");
        bool success = SaveManager.LoadFromPath(path);

        if (success)
        {
            // Ha sikerült, betöltjük a játékot
            SceneManager.LoadScene("WorldMapScene");
        }
        else
        {
            Debug.LogWarning("Sikertelen betöltés (vagy Game Over-es mentés).");
            // Itt esetleg kiírhatnál egy hibaüzenetet a képernyõre
        }
    }
}