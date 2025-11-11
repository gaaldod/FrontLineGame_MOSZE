using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>
/// Simple in-game Save Browser: builds a modal UI listing save files from SaveManager.GetSaveFiles().
/// Clicking an entry will load that save (via SaveManager.LoadFromPath) and open the world scene.
/// Instantiated/used from MainMenu when player presses "Saves".
/// </summary>
public class SaveBrowser : MonoBehaviour
{
    private GameObject rootCanvas;
    private RectTransform contentRect;
    private ScrollRect scrollRectField;

    const float EntryHeight = 24f;
    const float EntrySpacing = 6f;

    public static void Open()
    {
        // ensure only one instance
        var existing = UnityEngine.Object.FindFirstObjectByType<SaveBrowser>();
        if (existing != null)
        {
            existing.Refresh();
            return;
        }

        var go = new GameObject("SaveBrowser");
        DontDestroyOnLoad(go);
        go.AddComponent<SaveBrowser>();
    }

    void Awake()
    {
        CreateUI();
        Refresh();
    }

    void CreateUI()
    {
        // Ensure EventSystem exists
        if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
            DontDestroyOnLoad(es);
        }

        // Canvas
        rootCanvas = new GameObject("SaveBrowserCanvas");
        rootCanvas.transform.SetParent(this.transform, false);
        var canvas = rootCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        rootCanvas.AddComponent<CanvasScaler>();
        rootCanvas.AddComponent<GraphicRaycaster>();

        // Panel background (semi-opaque dark overlay)
        var panelGO = CreateUIElement("Panel", rootCanvas.transform);
        var image = panelGO.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.6f);
        var panelRect = panelGO.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // Centered window - made light so content is visible
        var window = CreateUIElement("Window", panelGO.transform);
        var winImg = window.AddComponent<Image>();
        winImg.color = new Color(0.95f, 0.95f, 0.95f, 0.98f); // light background (was dark)
        var winRect = window.GetComponent<RectTransform>();
        winRect.sizeDelta = new Vector2(600, 500);
        winRect.anchorMin = new Vector2(0.5f, 0.5f);
        winRect.anchorMax = new Vector2(0.5f, 0.5f);
        winRect.anchoredPosition = Vector2.zero;

        // Title
        var title = CreateText("Select Save", window.transform, 20, TextAnchor.UpperCenter);
        var titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0, -10);
        titleRect.sizeDelta = new Vector2(0, 30);

        // Close button (fixed size, don't stretch)
        var closeBtn = CreateButton("Close", window.transform, new Vector2(80, 30));
        var cbRect = closeBtn.GetComponent<RectTransform>();
        cbRect.anchorMin = new Vector2(1f, 1f);
        cbRect.anchorMax = new Vector2(1f, 1f);
        cbRect.pivot = new Vector2(1f, 1f);
        cbRect.anchoredPosition = new Vector2(-50, -20);
        cbRect.sizeDelta = new Vector2(80, 30);
        var cbLayout = closeBtn.GetComponent<LayoutElement>();
        if (cbLayout != null)
        {
            cbLayout.preferredHeight = 30;
            cbLayout.flexibleWidth = 0;
        }
        closeBtn.GetComponentInChildren<Text>().text = "Close";
        closeBtn.onClick.AddListener(() => { Destroy(this.gameObject); });

        // Scroll view
        var scrollGO = CreateUIElement("ScrollView", window.transform);
        scrollRectField = scrollGO.AddComponent<ScrollRect>();
        var svImage = scrollGO.AddComponent<Image>();
        svImage.color = new Color(0.97f, 0.97f, 0.97f, 1f); // make viewport area light as well
        var svRect = scrollGO.GetComponent<RectTransform>();
        svRect.anchorMin = new Vector2(0f, 0f);
        svRect.anchorMax = new Vector2(1f, 1f);
        svRect.offsetMin = new Vector2(20, 20);
        svRect.offsetMax = new Vector2(-20, -70);

        // Viewport (mask)
        var viewport = CreateUIElement("Viewport", scrollGO.transform);
        var vpImage = viewport.AddComponent<Image>();
        vpImage.color = new Color(0, 0, 0, 0);
        var mask = viewport.AddComponent<Mask>();
        mask.showMaskGraphic = false;
        var vpRect = viewport.GetComponent<RectTransform>();
        vpRect.anchorMin = Vector2.zero;
        vpRect.anchorMax = Vector2.one;
        vpRect.offsetMin = Vector2.zero;
        vpRect.offsetMax = Vector2.zero;
        scrollRectField.viewport = vpRect;

        // Content
        var content = CreateUIElement("Content", viewport.transform);
        contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0, 0);

        // Vertical layout for entries
        var layout = content.AddComponent<VerticalLayoutGroup>();
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        layout.spacing = EntrySpacing;
        var fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRectField.content = contentRect;

        // Footer: Open folder button (fixed size)
        var openFolderBtn = CreateButton("OpenFolder", window.transform, new Vector2(140, 30));
        var ofRect = openFolderBtn.GetComponent<RectTransform>();
        ofRect.anchorMin = new Vector2(0f, 0f);
        ofRect.anchorMax = new Vector2(0f, 0f);
        ofRect.pivot = new Vector2(0f, 0f);
        ofRect.anchoredPosition = new Vector2(80, 20);
        ofRect.sizeDelta = new Vector2(140, 30);
        var ofLayout = openFolderBtn.GetComponent<LayoutElement>();
        if (ofLayout != null)
        {
            ofLayout.preferredHeight = 30;
            ofLayout.flexibleWidth = 0;
        }
        openFolderBtn.GetComponentInChildren<Text>().text = "Open Folder";
        openFolderBtn.onClick.AddListener(() => SaveManager.OpenSavesFolder());

        // Footer: Refresh button (fixed size)
        var refreshBtn = CreateButton("Refresh", window.transform, new Vector2(100, 30));
        var rfRect = refreshBtn.GetComponent<RectTransform>();
        rfRect.anchorMin = new Vector2(1f, 0f);
        rfRect.anchorMax = new Vector2(1f, 0f);
        rfRect.pivot = new Vector2(1f, 0f);
        rfRect.anchoredPosition = new Vector2(-80, 20);
        rfRect.sizeDelta = new Vector2(100, 30);
        var rfLayout = refreshBtn.GetComponent<LayoutElement>();
        if (rfLayout != null)
        {
            rfLayout.preferredHeight = 30;
            rfLayout.flexibleWidth = 0;
        }
        refreshBtn.GetComponentInChildren<Text>().text = "Refresh";
        refreshBtn.onClick.AddListener(Refresh);
    }

    void Refresh()
    {
        // clear existing children in content
        foreach (Transform child in contentRect)
            Destroy(child.gameObject);

        string[] files = SaveManager.GetSaveFiles();

        Debug.Log($"SaveBrowser.Refresh: files length = {(files == null ? -1 : files.Length)}");

        if (files == null || files.Length == 0)
        {
            var noText = CreateText("No saves found", contentRect, 16, TextAnchor.MiddleCenter);
            var rt = noText.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 40);
            return;
        }

        foreach (var path in files)
        {
            Debug.Log($"SaveBrowser.Refresh: listing {path}");
            var fi = new FileInfo(path);
            string label = $"{fi.Name}  ({fi.LastWriteTime.ToLocalTime():yyyy-MM-dd HH:mm})";

            var btn = CreateButton("Entry", contentRect, new Vector2(0, EntryHeight));
            // make entry backgrounds more visible
            var entryImage = btn.GetComponent<Image>();
            if (entryImage != null)
                entryImage.color = new Color(0.36f, 0.36f, 0.36f, 1f); // darker entry on light window

            var txt = btn.GetComponentInChildren<Text>();
            if (txt != null)
            {
                txt.text = label;
                txt.fontSize = 14; // ensure readable
                txt.color = Color.black; // use dark text on light background
            }

            string capturedPath = path;
            btn.onClick.AddListener(() =>
            {
                bool ok = SaveManager.LoadFromPath(capturedPath);
                if (!ok)
                {
                    Debug.LogWarning($"SaveBrowser: failed to load {capturedPath}");
                    return;
                }
                Debug.Log($"SaveBrowser: loaded {capturedPath}, opening world scene.");
                SceneManager.LoadScene("WorldMapScene");
            });
        }

        // Force layout update so ContentSizeFitter / VerticalLayoutGroup calculate sizes now
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);

        // Fallback: if content height still zero, set approximate height so viewport can show children
        if (contentRect.rect.height <= 0f)
        {
            float fallbackHeight = files.Length * (EntryHeight + EntrySpacing);
            contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, fallbackHeight);
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
            Debug.Log($"SaveBrowser.Refresh: applied fallback content height = {fallbackHeight}");
        }

        // Ensure scroll shows top
        if (scrollRectField != null)
            scrollRectField.verticalNormalizedPosition = 1f;

        // Diagnostic: log content rect and children rects so you can inspect layout at runtime
        Debug.Log($"SaveBrowser.Refresh: contentRect.rect = {contentRect.rect}, childCount = {contentRect.childCount}");
        for (int i = 0; i < contentRect.childCount; i++)
        {
            var child = contentRect.GetChild(i) as RectTransform;
            if (child != null)
                Debug.Log($"  child[{i}] name={child.name} anchoredPos={child.anchoredPosition} sizeDelta={child.sizeDelta} rect={child.rect}");
        }
    }

    // utility helpers
    GameObject CreateUIElement(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.localScale = Vector3.one;
        return go;
    }

    Button CreateButton(string name, Transform parent, Vector2 size)
    {
        var go = CreateUIElement(name, parent);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        var btn = go.AddComponent<Button>();

        var rt = go.GetComponent<RectTransform>();
        // stretch horizontally, anchor to top for layout
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);

        // Add LayoutElement so VerticalLayoutGroup can size this entry correctly
        var layout = go.AddComponent<LayoutElement>();
        layout.preferredHeight = size.y;
        layout.flexibleWidth = 1;

        var txtGO = CreateUIElement("Text", go.transform);
        var text = txtGO.AddComponent<Text>();

        // Prefer a dynamic OS font (Arial) so text renders reliably in builds.
        Font dyn = null;
        try { dyn = Font.CreateDynamicFontFromOSFont("Arial", Mathf.Max(14, Mathf.RoundToInt(size.y))); } catch { dyn = null; }
        if (dyn != null)
            text.font = dyn;
        else
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.black; // dark text for light window
        text.enabled = true;

        var trt = txtGO.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;
        return btn;
    }

    Text CreateText(string textString, Transform parent, int fontSize = 14, TextAnchor anchor = TextAnchor.MiddleCenter)
    {
        var go = CreateUIElement("Text", parent);
        var txt = go.AddComponent<Text>();

        // Prefer a dynamic OS font (Arial) so text renders reliably in builds.
        Font dyn = null;
        try { dyn = Font.CreateDynamicFontFromOSFont("Arial", Mathf.Max(14, fontSize)); } catch { dyn = null; }
        if (dyn != null)
            txt.font = dyn;
        else
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        txt.text = textString;
        txt.alignment = anchor;
        txt.fontSize = fontSize;
        txt.color = Color.black; // dark text for light window
        txt.enabled = true;

        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 24);
        return txt;
    }
}