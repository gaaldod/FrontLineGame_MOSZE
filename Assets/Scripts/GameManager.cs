using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    private void Awake() => Instance = this;

    [Header("Prefabs & UI")]
    public GameObject unitPrefab;
    public Button buyLeftButton;
    public Button buyRightButton;
    public Button startBattleButton;
    public TMP_Text leftGoldText;
    public TMP_Text rightGoldText;

    [Header("Economy Settings")]
    public int startingGold = 15;
    public int unitCost = 5;

    [Header("Player Colors")]
    public Color leftPlayerColor = Color.red;
    public Color rightPlayerColor = Color.cyan;

    private int[] gold = new int[2];
    private bool isPlacingUnit = false;
    private GameObject ghostUnit;
    private int activePlayer = 0;

    public int ActivePlayer => activePlayer;

    void Start()
    {
        // arany átvétele a WorldManagerből
        if (WorldManager.Instance != null)
        {
            gold = (int[])WorldManager.Instance.GetGold().Clone();
            Debug.Log($"Atvett arany: Bal={gold[0]}, Jobb={gold[1]}");
        }
        else
        {
            gold[0] = startingGold;
            gold[1] = startingGold;
        }

        UpdateGoldUI();

        Debug.Log($"Setting up buttons - buyLeftButton: {buyLeftButton != null}, buyRightButton: {buyRightButton != null}, startBattleButton: {startBattleButton != null}");

        if (buyLeftButton != null)
            buyLeftButton.onClick.AddListener(() => StartPlacingUnit(0));
        if (buyRightButton != null)
            buyRightButton.onClick.AddListener(() => StartPlacingUnit(1));
        
        // Try to find button if not assigned
        if (startBattleButton == null)
        {
            Debug.LogWarning("startBattleButton not assigned, trying to find it by name...");
            GameObject buttonObj = GameObject.Find("StartBattleButton");
            if (buttonObj != null)
            {
                startBattleButton = buttonObj.GetComponent<Button>();
                if (startBattleButton != null)
                {
                    Debug.Log("Found StartBattleButton and assigned it!");
                }
            }
        }
        
        if (startBattleButton != null)
        {
            Debug.Log("Wiring up startBattleButton onClick listener");
            startBattleButton.onClick.AddListener(StartBattle);
            
            // Verify button is interactable
            if (!startBattleButton.interactable)
            {
                Debug.LogWarning("startBattleButton is not interactable! Enabling it...");
                startBattleButton.interactable = true;
            }
        }
        else
        {
            Debug.LogError("startBattleButton is NULL! Please assign it in the Inspector.");
        }
    }

    public void StartBattle()
    {
        Debug.Log("GameManager.StartBattle() called - Button was clicked!");
        
        if (BattleManager.Instance != null)
        {
            BattleManager.Instance.StartBattle();
            Debug.Log("Battle started!");
        }
        else
        {
            Debug.LogError("BattleManager not found! Make sure BattleManager component exists in the scene.");
        }
    }

    void Update()
    {
        if (isPlacingUnit && ghostUnit != null)
        {
            FollowMouse();
            if (Input.GetKeyDown(KeyCode.Space))
                CancelPlacingUnit();
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow))
            EndGame(0);
        if (Input.GetKeyDown(KeyCode.RightArrow))
            EndGame(1);
    }

    public void EndGame(int winner)
    {
        Debug.Log($"Jatekos {winner + 1} NYERT!");
        gold[winner] += 5;

        if (WorldManager.Instance != null)
        {
            WorldManager.Instance.SetGold(gold);
            WorldManager.Instance.RecordBattleResult(winner);
        }

        SceneManager.LoadScene("WorldMapScene");
    }

    //SKELETON (GD) Public helper to read a player's current gold (avoiding softlocks)
    public int GetGold(int player)
    {
        if (player < 0 || player >= gold.Length) return 0;
        return gold[player];
    }
    //SKELETON(GD) END
    void CancelPlacingUnit()
    {
        if (!isPlacingUnit) return;
        if (ghostUnit != null) Destroy(ghostUnit);
        isPlacingUnit = false;
    }

    void StartPlacingUnit(int player)
    {
        if (isPlacingUnit) return;

        if (gold[player] < unitCost)
        {
            Debug.Log($"Jatekos {player + 1} nem engedheti meg maganak a unitot!");
            return;
        }

        isPlacingUnit = true;
        activePlayer = player;

        ghostUnit = Instantiate(unitPrefab);
        SetTransparency(ghostUnit, 0.5f);

        Vector3 startPos = (activePlayer == 0)
            ? new Vector3(-2f, 0.15f, 0f)
            : new Vector3(10f, 0.15f, 0f);

        ghostUnit.transform.position = startPos;
    }

    void FollowMouse()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        int layerMask = LayerMask.GetMask("LeftZone", "RightZone");

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, layerMask))
        {
            Vector3 mouseOffset = Camera.main.transform.right * 1f + Vector3.up * 0.1f;
            Vector3 targetPos = hit.point + mouseOffset;

            ghostUnit.transform.position = Vector3.Lerp(
                ghostUnit.transform.position,
                targetPos,
                Time.deltaTime * 20f
            );
        }
    }

    public void TryPlaceUnit(HexTile tile)
    {
        if (!isPlacingUnit || tile.isOccupied) return;
        if (tile.CompareTag("Castle")) return;

        if (activePlayer == 0 && tile.gameObject.layer != LayerMask.NameToLayer("LeftZone"))
        {
            Debug.Log("A bal játékos csak a bal oldalon rakhat le unitot!");
            return;
        }

        if (activePlayer == 1 && tile.gameObject.layer != LayerMask.NameToLayer("RightZone"))
        {
            Debug.Log("A jobb játékos csak a jobb oldalon rakhat le unitot!");
            return;
        }

        gold[activePlayer] -= unitCost;
        UpdateGoldUI();

        tile.isOccupied = true;
        Instantiate(unitPrefab, tile.transform.position + Vector3.up * 0.1f, Quaternion.identity);

        Destroy(ghostUnit);
        isPlacingUnit = false;
        ghostUnit = null;

        Debug.Log($"Jatekos {activePlayer + 1} unitot helyezett le!");
    }

    void UpdateGoldUI()
    {
        if (leftGoldText != null)
            leftGoldText.text = $"Arany: {gold[0]}";
        if (rightGoldText != null)
            rightGoldText.text = $"Arany: {gold[1]}";
    }

    void SetTransparency(GameObject obj, float alpha)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            foreach (Material m in r.materials)
            {
                Color c = m.color;
                c.a = alpha;
                m.color = c;
                m.SetFloat("_Mode", 3);
                m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                m.SetInt("_ZWrite", 0);
                m.DisableKeyword("_ALPHATEST_ON");
                m.EnableKeyword("_ALPHABLEND_ON");
                m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                m.renderQueue = 3000;
            }
        }
    }
}
