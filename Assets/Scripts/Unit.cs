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

    [Header("Range Settings")]
    public int attackRange = 1;

    [Header("Model Settings")]
    [Tooltip("Model prefab to instantiate for this unit (e.g., archer model). Drag the model from Project window here.")]
    public GameObject unitModelPrefab;
    [Tooltip("Animator Controller for unit animations. Leave empty to use Animator from model prefab.")]
    public RuntimeAnimatorController animatorController;

    private UnitHealthBar healthBar;
    private GameObject instantiatedModel;
    private Animator unitAnimator;
    private int unitOwner = -1; // 0 = left, 1 = right, -1 = not determined

    void Start()
    {
        targetPosition = transform.position;
        currentHealth = maxHealth;
        
        // Determine unit owner based on position/layer
        DetermineUnitOwner();
        
        // Instantiate model if provided (for archer units and melee units)
        // Model prefab is OPTIONAL - if null or invalid, unit will work without a visual model
        if (unitModelPrefab != null)
        {
            // Debug: Check what we're instantiating
            Debug.Log($"Unit {gameObject.name}: Instantiating model prefab: {unitModelPrefab.name}");
            
            instantiatedModel = Instantiate(unitModelPrefab, transform);
            instantiatedModel.transform.localPosition = Vector3.zero;
            instantiatedModel.transform.localRotation = Quaternion.identity;
            instantiatedModel.transform.localScale = Vector3.one;
            
            // Check if the instantiated model IS a TextMeshPro (wrong prefab assigned or wrong fileID)
            TMPro.TextMeshPro rootText = instantiatedModel.GetComponent<TMPro.TextMeshPro>();
            Renderer meshRenderer = instantiatedModel.GetComponent<Renderer>();
            
            // Set up Animator only if we have a valid model
            if (instantiatedModel != null)
            {
                SetupAnimator();
                SetFacingDirection();
            }
        }
        
        // Create healthbar
        CreateHealthBar();
    }
    
    void DetermineUnitOwner()
    {
        // Check tile layer first
        int leftLayer = LayerMask.NameToLayer("LeftZone");
        int rightLayer = LayerMask.NameToLayer("RightZone");
        
        // Raycast down to find the tile
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 2f))
        {
            if (hit.collider.gameObject.layer == leftLayer)
            {
                unitOwner = 0; // Left player
                return;
            }
            else if (hit.collider.gameObject.layer == rightLayer)
            {
                unitOwner = 1; // Right player
                return;
            }
        }
        
        // Fallback: use position (left side of map = left player)
        // Assuming map center is around x=0, left is negative, right is positive
        unitOwner = transform.position.x < 0 ? 0 : 1;
    }
    
    void SetupAnimator()
    {
        if (instantiatedModel == null) return;
        
        // Try to find Animator in the instantiated model or its children
        unitAnimator = instantiatedModel.GetComponent<Animator>();
        if (unitAnimator == null)
        {
            unitAnimator = instantiatedModel.GetComponentInChildren<Animator>();
        }
        
        // If no Animator found, add one to the model
        if (unitAnimator == null)
        {
            unitAnimator = instantiatedModel.AddComponent<Animator>();
        }
        
        // Assign Animator Controller if provided
        if (animatorController != null && unitAnimator != null)
        {
            unitAnimator.runtimeAnimatorController = animatorController;
        }

    }
    
    void SetFacingDirection()
    {
        if (instantiatedModel == null) return;
        
        // Left side (player 0) faces right toward enemy, Right side (player 1) faces left toward enemy
        // In Unity: 0 = forward (Z+), 90 = right (X+), 180 = back (Z-), 270 = left (X-)
        float yRotation;
        if (unitOwner == 0)
        {
            // Left player: face right (positive X direction) = 90 degrees
            yRotation = 90f;
        }
        else
        {
            // Right player: face left (negative X direction) = 270 degrees (or -90)
            yRotation = 270f;
        }
        instantiatedModel.transform.localRotation = Quaternion.Euler(0, yRotation, 0);
    }

    void Update()
    {
        bool wasMoving = Vector3.Distance(transform.position, targetPosition) > 0.01f;
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
        bool isMoving = Vector3.Distance(transform.position, targetPosition) > 0.01f;
        
        // Update animation based on movement state
        if (unitAnimator != null)
        {
            if (isMoving)
            {
                if (HasAnimatorParameter("Speed"))
                    unitAnimator.SetFloat("Speed", moveSpeed);
                if (HasAnimatorParameter("IsWalking"))
                    unitAnimator.SetBool("IsWalking", true);
            }
            else if (wasMoving && !isMoving)
            {
                // Just stopped moving
                if (HasAnimatorParameter("IsWalking"))
                    unitAnimator.SetBool("IsWalking", false);
                if (HasAnimatorParameter("Speed"))
                    unitAnimator.SetFloat("Speed", 0f);
            }
        }
    }

    public void MoveTo(Vector3 position)
    {
        targetPosition = position;
        
        // Play walk animation if animator exists
        if (unitAnimator != null)
        {
            // Try common animation parameter names
            if (HasAnimatorParameter("Speed"))
                unitAnimator.SetFloat("Speed", moveSpeed);
            if (HasAnimatorParameter("IsWalking"))
                unitAnimator.SetBool("IsWalking", true);
            if (HasAnimatorParameter("Walk"))
                unitAnimator.SetTrigger("Walk");
        }
    }
    
    bool HasAnimatorParameter(string paramName)
    {
        if (unitAnimator == null || unitAnimator.runtimeAnimatorController == null)
            return false;
        
        foreach (AnimatorControllerParameter param in unitAnimator.parameters)
        {
            if (param.name == paramName)
                return true;
        }
        return false;
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);
        
        if (healthBar != null)
            healthBar.UpdateHealth(currentHealth, maxHealth);
    }
    
    public void PlayAttackAnimation()
    {
        if (unitAnimator != null)
        {
            // Try common attack animation parameter names
            if (HasAnimatorParameter("Attack"))
                unitAnimator.SetTrigger("Attack");
            if (HasAnimatorParameter("IsAttacking"))
                unitAnimator.SetBool("IsAttacking", true);
        }
    }

    public void Die()
    {
        if (healthBar != null)
            Destroy(healthBar.gameObject);
        
        if (instantiatedModel != null)
            Destroy(instantiatedModel);
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