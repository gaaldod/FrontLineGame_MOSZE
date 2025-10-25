using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

/// <summary>
/// Simple runtime UI that builds a visible HUD on scene start so you see a UI immediately when pressing Play.
/// - Exposes a UnityEvent 'onStartPressed' so you can hook existing map/spawn methods in the Inspector.
/// - If a MapGenerator3D-like component exists it will attempt a non-failing SendMessage call for convenience.
/// </summary>
[DisallowMultipleComponent]
public class UIManager : MonoBehaviour
{
    [Header("Runtime UI settings")]
    [Tooltip("Left team label text")]
    public string leftTeamName = "Allies";
    [Tooltip("Right team label text")]
    public string rightTeamName = "Enemies";

    [Header("Events")]
    public UnityEvent onStartPressed; // Hook MapGenerator3D or spawner methods in Inspector

    // Internal state
    private int leftScore;
    private int rightScore;

    // Runtime refs
    private Text leftScoreText;
    private Text rightScoreText;
    private Button startButton;
    private Text statusText;

    void Awake()
    {
        BuildUI();
        UpdateScores();
    }

    void BuildUI()
    {
        // Root Canvas
        var canvasGO = new GameObject("UI_Canvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600, 900);
        canvasGO.AddComponent<GraphicRaycaster>();
        DontDestroyOnLoad(canvasGO);

        // Background panel (top bar)
        var bg = CreateUIBlock(canvasGO.transform, "HUD_Background");
        var bgImage = bg.AddComponent<Image>();
        bgImage.color = new Color32(24, 24, 24, 200);
        var rect = bg.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(0, 110);
        rect.anchoredPosition = new Vector2(0, 0);

        // Left team panel
        var leftPanel = CreateUIBlock(bg.transform, "LeftPanel");
        var lpRect = leftPanel.GetComponent<RectTransform>();
        lpRect.anchorMin = new Vector2(0f, 0f);
        lpRect.anchorMax = new Vector2(0f, 1f);
        lpRect.pivot = new Vector2(0f, 0.5f);
        lpRect.sizeDelta = new Vector2(300, 0);
        lpRect.anchoredPosition = new Vector2(10, 0);

        var leftLabel = CreateText(leftPanel.transform, "LeftLabel", leftTeamName, 18, TextAnchor.UpperLeft);
        leftLabel.rectTransform.anchoredPosition = new Vector2(10, -10);
        leftScoreText = CreateText(leftPanel.transform, "LeftScore", "0", 36, TextAnchor.LowerLeft);
        leftScoreText.rectTransform.anchoredPosition = new Vector2(10, 10);

        // Right team panel
        var rightPanel = CreateUIBlock(bg.transform, "RightPanel");
        var rpRect = rightPanel.GetComponent<RectTransform>();
        rpRect.anchorMin = new Vector2(1f, 0f);
        rpRect.anchorMax = new Vector2(1f, 1f);
        rpRect.pivot = new Vector2(1f, 0.5f);
        rpRect.sizeDelta = new Vector2(300, 0);
        rpRect.anchoredPosition = new Vector2(-10, 0);

        var rightLabel = CreateText(rightPanel.transform, "RightLabel", rightTeamName, 18, TextAnchor.UpperRight);
        rightLabel.rectTransform.anchoredPosition = new Vector2(-10, -10);
        rightScoreText = CreateText(rightPanel.transform, "RightScore", "0", 36, TextAnchor.LowerRight);
        rightScoreText.rectTransform.anchoredPosition = new Vector2(-10, 10);

        // Center controls
        var centerPanel = CreateUIBlock(bg.transform, "CenterPanel");
        var cpRect = centerPanel.GetComponent<RectTransform>();
        cpRect.anchorMin = new Vector2(0.5f, 0f);
        cpRect.anchorMax = new Vector2(0.5f, 1f);
        cpRect.pivot = new Vector2(0.5f, 0.5f);
        cpRect.sizeDelta = new Vector2(420, 0);
        cpRect.anchoredPosition = new Vector2(0, 0);

        // Start button
        startButton = CreateButton(centerPanel.transform, "StartButton", "Start Test");
        startButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 10);
        startButton.onClick.AddListener(OnStartButtonPressed);

        // Status text below button
        statusText = CreateText(centerPanel.transform, "StatusText", "Idle", 16, TextAnchor.UpperCenter);
        statusText.rectTransform.anchoredPosition = new Vector2(0, -40);
    }

    // Helpers to create UI elements
    private GameObject CreateUIBlock(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.localScale = Vector3.one;
        return go;
    }

    private Text CreateText(Transform parent, string name, string content, int fontSize, TextAnchor anchor)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var txt = go.AddComponent<Text>();
        txt.text = content;
        txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        txt.fontSize = fontSize;
        txt.alignment = anchor;
        txt.color = Color.white;
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(400, 36);
        return txt;
    }

    private Button CreateButton(Transform parent, string name, string label)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var img = go.AddComponent<Image>();
        img.color = new Color32(70, 130, 200, 255);

        var btn = go.AddComponent<Button>();
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(160, 36);

        var txt = CreateText(go.transform, "Label", label, 18, TextAnchor.MiddleCenter);
        txt.rectTransform.sizeDelta = rt.sizeDelta;
        return btn;
    }

    // UI logic
    public void IncrementLeft(int by = 1)
    {
        leftScore += by;
        UpdateScores();
    }

    public void IncrementRight(int by = 1)
    {
        rightScore += by;
        UpdateScores();
    }

    void UpdateScores()
    {
        if (leftScoreText != null) leftScoreText.text = leftScore.ToString();
        if (rightScoreText != null) rightScoreText.text = rightScore.ToString();
    }

    void OnStartButtonPressed()
    {
        statusText.text = "Start pressed — invoking hooks...";
        onStartPressed?.Invoke();

        // Try to auto-call a MapGenerator-like method if present somewhere in scene.
        // This uses SendMessage and won't throw an error if the method doesn't exist.
        // Common candidate names: "GenerateMap", "SpawnUnits", "StartMatch"
        var rootObjects = gameObject.scene.GetRootGameObjects();
        foreach (var root in rootObjects)
        {
            root.SendMessage("GenerateMap", SendMessageOptions.DontRequireReceiver);
            root.SendMessage("SpawnUnits", SendMessageOptions.DontRequireReceiver);
            root.SendMessage("StartMatch", SendMessageOptions.DontRequireReceiver);
        }

        // Simple demo: simulate a few score changes so you can see UI reacting
        leftScore = 0;
        rightScore = 0;
        StartCoroutine(DemoScoreCoroutine());
    }

    IEnumerator DemoScoreCoroutine()
    {
        for (int i = 0; i < 6; i++)
        {
            yield return new WaitForSeconds(0.6f);
            if (i % 2 == 0) IncrementLeft(Random.Range(0, 2));
            else IncrementRight(Random.Range(0, 2));
        }
        statusText.text = "Demo finished";
    }
}