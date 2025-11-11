using UnityEngine;
using UnityEngine.UI;

public class UnitHealthBar : MonoBehaviour
{
    private RectTransform fillRect;
    private Image fillImage;
    private int maxHealth;

    public void Initialize(RectTransform fillRectTransform, int currentHealth, int maxHealthValue)
    {
        fillRect = fillRectTransform;
        fillImage = fillRect.GetComponent<Image>();
        maxHealth = maxHealthValue;
        UpdateHealth(currentHealth, maxHealth);
    }

    public void UpdateHealth(int currentHealth, int maxHealthValue)
    {
        if (fillRect == null) return;

        maxHealth = maxHealthValue;
        float healthPercent = maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;
        fillRect.localScale = new Vector3(healthPercent, 1f, 1f);

        // Change color based on health percentage
        if (fillImage != null)
        {
            if (healthPercent > 0.6f)
                fillImage.color = Color.green;
            else if (healthPercent > 0.3f)
                fillImage.color = Color.yellow;
            else
                fillImage.color = Color.red;
        }

        // Face the camera
        if (Camera.main != null)
        {
            transform.LookAt(Camera.main.transform);
            transform.Rotate(0, 180, 0); // Flip to face camera
        }
    }

    void LateUpdate()
    {
        // Always face the camera
        if (Camera.main != null)
        {
            transform.LookAt(Camera.main.transform);
            transform.Rotate(0, 180, 0);
        }
    }
}

