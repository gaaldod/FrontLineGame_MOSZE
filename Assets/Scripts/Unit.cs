using UnityEngine;
using UnityEngine.UI;

public class Unit : MonoBehaviour
{
    public float moveSpeed = 3f;
    public Vector3 targetPosition;
    
    [Header("Combat Stats")]
    public int maxHealth = 5;
    public int currentHealth = 5;
    public int attackDamage = 1;

    private UnitHealthBar healthBar;

    void Start()
    {
        targetPosition = transform.position;
        currentHealth = maxHealth;
        
        // Create healthbar
        CreateHealthBar();
    }

    void Update()
    {
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
    }

    public void MoveTo(Vector3 position)
    {
        targetPosition = position;
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);
        
        if (healthBar != null)
            healthBar.UpdateHealth(currentHealth, maxHealth);
    }

    public void Die()
    {
        if (healthBar != null)
            Destroy(healthBar.gameObject);
    }

    void CreateHealthBar()
    {
        // Create a canvas for world space UI
        GameObject canvasGO = new GameObject("HealthBarCanvas");
        canvasGO.transform.SetParent(transform);
        canvasGO.transform.localPosition = Vector3.up * 1.2f; // Above the unit
        canvasGO.transform.localRotation = Quaternion.identity;
        canvasGO.transform.localScale = Vector3.one * 0.01f; // Scale down for world space

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // Create healthbar background
        GameObject bgGO = new GameObject("Background");
        bgGO.transform.SetParent(canvasGO.transform, false);
        RectTransform bgRect = bgGO.AddComponent<RectTransform>();
        bgRect.sizeDelta = new Vector2(100, 10);
        bgRect.anchoredPosition = Vector2.zero;
        
        Image bgImage = bgGO.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        // Create healthbar fill
        GameObject fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(bgGO.transform, false);
        RectTransform fillRect = fillGO.AddComponent<RectTransform>();
        fillRect.sizeDelta = new Vector2(100, 10);
        fillRect.anchoredPosition = Vector2.zero;
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2(1, 1);
        fillRect.pivot = new Vector2(0, 0.5f);
        
        Image fillImage = fillGO.AddComponent<Image>();
        fillImage.color = Color.green;

        // Add healthbar component
        healthBar = canvasGO.AddComponent<UnitHealthBar>();
        healthBar.Initialize(fillRect, currentHealth, maxHealth);
    }

    public bool IsAlive()
    {
        return currentHealth > 0;
    }
}