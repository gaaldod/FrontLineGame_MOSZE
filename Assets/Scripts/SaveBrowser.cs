using UnityEngine;
using UnityEngine.UI;
using System.IO;
using UnityEngine.SceneManagement;
using TMPro;

public class SaveBrowser : MonoBehaviour
{
    [Header("UI Referenci�k (H�zd be �ket!)")]
    public RectTransform contentContainer; // A Scroll View -> Content objektuma
    public GameObject saveItemPrefab;  // A Gomb prefab, amit a Projectb�l h�zol be
    public ScrollRect scrollView;

    [Header("Funkci� Gombok (H�zd be a Panelr�l!)")]
    public Button openFolderButton;    // A mappa megnyit�sa gomb
    public Button refreshButton;       // A friss�t�s gomb

    private void OnEnable()
    {
        Refresh();
    }
    public void Open()
    {
        gameObject.SetActive(true);
    }

    // Ezt h�vja a Close gomb
    public void Close()
    {
        gameObject.SetActive(false);
    }

    void Start()
    {
        // Bek�tj�k a gombokat automatikusan, ha be vannak h�zva az Inspectorban
        if (openFolderButton != null)
            openFolderButton.onClick.AddListener(OpenFolder);

        if (refreshButton != null)
            refreshButton.onClick.AddListener(Refresh);
    }

    // A mappa megnyit�sa
    void OpenFolder()
    {
        SaveManager.OpenSavesFolder();
    }

    // A lista friss�t�se (ez a l�nyeg)
    public void Refresh()
    {
        // 1. T�r�lj�k a r�gi list�t (hogy ne dupl�z�djon)
        foreach (Transform child in contentContainer)
        {
            Destroy(child.gameObject);
        }

        // 2. Lek�rj�k a f�jlokat a SaveManager-t�l
        string[] files = SaveManager.GetSaveFiles();

        if (files == null || files.Length == 0)
        {
            Debug.Log("Nincsenek ment�sek.");
            return;
        }

        // 3. Legy�rtjuk a gombokat a Prefab alapj�n
        foreach (string path in files)
        {
            // L�trehozunk egy �j gombot a Content alatt
            GameObject newButton = Instantiate(saveItemPrefab, contentContainer);

            // Adatok lek�r�se
            var fi = new FileInfo(path);

            // Sz�veg be�ll�t�sa (Felt�telezz�k, hogy van Text komponens a gombon)
            TMP_Text btnText = newButton.GetComponentInChildren<TMP_Text>();
            if (btnText != null)
            {
                // Form�tum: F�jln�v (sort�r�s) D�tum
                btnText.text = $"{fi.Name}</size>";
            }

            // Kattint�s esem�ny bek�t�se (Bet�lt�s)
            string capturedPath = path; // Fontos a lambda miatt
            newButton.GetComponent<Button>().onClick.AddListener(() => LoadSave(capturedPath));
        }
        ResetScrollPosition();
    }
    void ResetScrollPosition()
    {
        if (scrollView != null)
        {
            // Ez a parancs k�nyszer�ti a Unity-t, hogy AZONNAL sz�molja ki a lista �j magass�g�t
            // Ha ez nincs itt, a g�rget�s "r�gi" adatokkal dolgozna �s pontatlan lenne.
            Canvas.ForceUpdateCanvases();

            // 1 = Teteje, 0 = Alja
            scrollView.verticalNormalizedPosition = 1f;
        }
    }

    void LoadSave(string path)
    {
        Debug.Log($"Bet�lt�s ind�t�sa: {path}");
        bool success = SaveManager.LoadFromPath(path);

        if (success)
        {
            // We are entering gameplay from the menu in this runtime; don't replay intro when returning to menu.
            MainMenu.SkipIntroForRestOfRuntime();
            // Ha siker�lt, bet�ltj�k a j�t�kot
            SceneManager.LoadScene("WorldMapScene");
        }
        else
        {
            Debug.LogWarning("Sikertelen bet�lt�s (vagy Game Over-es ment�s).");
            // Itt esetleg ki�rhatn�l egy hiba�zenetet a k�perny�re
        }
    }
}