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
    [Tooltip("Attack animation clip (optional, will try to load from FBX if not set).")]
    public AnimationClip attackAnimationClip;

    private UnitHealthBar healthBar;
    private GameObject instantiatedModel;
    private Animator unitAnimator;
    private Animation legacyAnimation; // For playing animations directly without Animator Controller
    private int unitOwner = -1; // 0 = left, 1 = right, -1 = not determined
    
    // Public method to set the unit owner directly (called by GameManager when placing units)
    public void SetOwner(int owner)
    {
        unitOwner = owner;
        // If model is already instantiated, update facing direction
        if (instantiatedModel != null)
        {
            SetFacingDirection();
        }
    }

    void Start()
    {
        targetPosition = transform.position;
        currentHealth = maxHealth;
        
        // Only determine unit owner if not already set (for units placed in battle scene)
        // Units placed in GameManager will have owner set via SetOwner() method
        if (unitOwner == -1)
        {
            DetermineUnitOwner();
        }
        
        // Instantiate model if provided (for archer units and melee units)
        // Model prefab is OPTIONAL - if null or invalid, unit will work without a visual model
        if (unitModelPrefab != null)
        {
            // Debug: Check what we're instantiating
            
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
        
        // Raycast down to find the tile - try with a longer distance and from higher up
        if (Physics.Raycast(transform.position + Vector3.up * 1f, Vector3.down, out RaycastHit hit, 5f))
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
            else
            {
            }
        }
        else
        {
        }
    }
    
    void SetupAnimator()
    {
        if (instantiatedModel == null)
        {
            Debug.LogWarning($"Unit {gameObject.name}: SetupAnimator called but instantiatedModel is null!");
            return;
        }
        
        Debug.Log($"Unit {gameObject.name}: Setting up animator for model '{instantiatedModel.name}'");
        
        // Try to find Animator in the instantiated model or its children
        unitAnimator = instantiatedModel.GetComponent<Animator>();
        if (unitAnimator == null)
        {
            unitAnimator = instantiatedModel.GetComponentInChildren<Animator>();
        }
        
        // If no Animator found, add one to the model
        if (unitAnimator == null)
        {
            Debug.Log($"Unit {gameObject.name}: No Animator found, adding one to '{instantiatedModel.name}'");
            unitAnimator = instantiatedModel.AddComponent<Animator>();
        }
        else
        {
            Debug.Log($"Unit {gameObject.name}: Found Animator on '{unitAnimator.gameObject.name}'");
        }
        
        // Set up Animator Controller
        if (unitAnimator != null)
        {
            // If a controller is provided, use it
            if (animatorController != null)
            {
                unitAnimator.runtimeAnimatorController = animatorController;
                Debug.Log($"Unit {gameObject.name}: Assigned Animator Controller '{animatorController.name}'");
            }
            // Otherwise, create a runtime controller using the base controller and attack clip
            else if (attackAnimationClip != null)
            {
                // Load base controller from Resources
                RuntimeAnimatorController baseController = Resources.Load<RuntimeAnimatorController>("Characters/KnightBaseController");
                
                if (baseController != null)
                {
                    // Create override controller and assign our attack clip to the "Attack" state
                    AnimatorOverrideController overrideController = new AnimatorOverrideController(baseController);
                    overrideController["Attack"] = attackAnimationClip;
                    unitAnimator.runtimeAnimatorController = overrideController;
                    Debug.Log($"Unit {gameObject.name}: Created runtime Animator Controller with attack clip '{attackAnimationClip.name}'");
                }
                else
                {
                    Debug.LogWarning($"Unit {gameObject.name}: KnightBaseController not found in Resources! Will try to play clip directly.");
                }
            }
            else
            {
                Debug.Log($"Unit {gameObject.name}: No Animator Controller or attack clip assigned");
            }
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
        // Raycast down to find the terrain height at the target position
        // This ensures units stay at the correct height above terrain when moving
        float terrainHeight = position.y;
        int layerMask = LayerMask.GetMask("LeftZone", "RightZone", "Default");
        
        // Raycast from above the target position to find terrain
        if (Physics.Raycast(position + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 5f, layerMask))
        {
            terrainHeight = hit.point.y;
        }
        
        // Set target position with the same offset used when spawning (0.25f above terrain)
        targetPosition = new Vector3(position.x, terrainHeight + 0.25f, position.z);
        
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
        Debug.Log($"Unit {gameObject.name}: PlayAttackAnimation() called");
        
        // Use Animator to play the attack animation
        if (unitAnimator != null)
        {
            // Diagnostics: log animator/controller/parameters to help debug missing animation
            var controller = unitAnimator.runtimeAnimatorController;
            string paramList = controller != null
                ? string.Join(", ", System.Array.ConvertAll(unitAnimator.parameters, p => $"{p.name}({p.type})"))
                : "none (no controller)";
            Debug.Log($"Unit {gameObject.name}: Animator present on '{unitAnimator.gameObject.name}', controller={(controller != null ? controller.name : "null")}, params=[{paramList}]");

            if (unitAnimator.runtimeAnimatorController != null)
            {
                // Check if attack animation is already playing - if so, don't restart it
                AnimatorStateInfo stateInfo = unitAnimator.GetCurrentAnimatorStateInfo(0);
                if (stateInfo.IsName("Attack") && stateInfo.normalizedTime < 1.0f)
                {
                    Debug.Log($"Unit {gameObject.name}: Attack animation already playing (normalized time: {stateInfo.normalizedTime:F2}), skipping restart");
                    return;
                }
                
                // Use SetTrigger("Attack") to properly trigger the transition from Idle to Attack
                // This matches the controller setup: Idle → Attack (triggered by "Attack" trigger)
                if (HasAnimatorParameter("Attack"))
                {
                    unitAnimator.SetTrigger("Attack");
                    Debug.Log($"Unit {gameObject.name}: Set 'Attack' trigger to play attack animation");
                }
                else
                {
                    // Fallback: try playing directly if trigger doesn't exist
                    unitAnimator.Play("Attack", 0, 0f);
                    Debug.Log($"Unit {gameObject.name}: No 'Attack' trigger found, playing 'Attack' state directly");
                }
            }
            else
            {
                Debug.LogWarning($"Unit {gameObject.name}: Animator has no controller assigned! Cannot play attack animation.");
            }
        }
        else
        {
            Debug.LogWarning($"Unit {gameObject.name}: No Animator component found! Cannot play attack animation.");
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