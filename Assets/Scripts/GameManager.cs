using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    private void Awake() => Instance = this;

    [Header("Basic Unit (Melee)")]
    public GameObject unitPrefab; // A sima kardos
    public int unitCost = 5;
    public Button buyLeftButton;
    public Button buyRightButton;

    [Header("Archer Unit (Ranged)")]
    public GameObject archerPrefab; // ÚJ: Ide húzd be az Archer prefabot
    public int archerCost = 10;     // ÚJ: Az íjász ára
    public Button buyArcherLeftButton;  // ÚJ: Gomb a bal oldali íjász vételhez
    public Button buyArcherRightButton; // ÚJ: Gomb a jobb oldali íjász vételhez

    [Header("UI & Settings")]
    public Button startBattleButton;
    public TMP_Text leftGoldText;
    public TMP_Text rightGoldText;
    public int startingGold = 15;

    [Header("Player Colors")]
    public Color leftPlayerColor = Color.red;
    public Color rightPlayerColor = Color.cyan;

    private int[] gold = new int[2];

    // ÁLLAPOT VÁLTOZÓK
    private bool isPlacingUnit = false;
    private GameObject ghostUnit;
    private int activePlayer = 0;

    // ÚJ: Eltároljuk, hogy éppen melyik egységet akarjuk lerakni
    private GameObject selectedUnitPrefab;
    private int selectedUnitCost;

    public int ActivePlayer => activePlayer;

    void Start()
    {
        // Arany átvétele a WorldManagerből
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

        // --- GOMBOK BEKÖTÉSE ---
        Debug.Log($"Setting up buttons - buyLeftButton: {buyLeftButton != null}, buyRightButton: {buyRightButton != null}, startBattleButton: {startBattleButton != null}");
        // Sima Unit (Kardos) gombok
        if (buyLeftButton != null)
            buyLeftButton.onClick.AddListener(() => StartPlacingUnit(0, unitPrefab, unitCost));

        if (buyRightButton != null)
            buyRightButton.onClick.AddListener(() => StartPlacingUnit(1, unitPrefab, unitCost));

        // ÚJ: Archer (Íjász) gombok
        if (buyArcherLeftButton != null)
            buyArcherLeftButton.onClick.AddListener(() => StartPlacingUnit(0, archerPrefab, archerCost));

        if (buyArcherRightButton != null)
            buyArcherRightButton.onClick.AddListener(() => StartPlacingUnit(1, archerPrefab, archerCost));

        // Start Battle gomb
        if (startBattleButton != null)
            startBattleButton.onClick.AddListener(StartBattle);
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

        //Win szimulálás
        if (Input.GetKeyDown(KeyCode.LeftArrow))
            EndGame(0);
        if (Input.GetKeyDown(KeyCode.RightArrow))
            EndGame(1);
    }

    public void EndGame(int winner)
    {
        Debug.Log($"Jatekos {winner + 1} NYERT!");

        // Kiszámoljuk, ki a vesztes (ha winner 0, akkor loser 1, és fordítva)
        int loser = (winner == 0) ? 1 : 0;

        // Nyertes kap 4 aranyat
        gold[winner] += 4;

        // Vesztes kap 7 aranyat
        gold[loser] += 7;

        if (WorldManager.Instance != null)
        {
            WorldManager.Instance.SetGold(gold);
            WorldManager.Instance.RecordBattleResult(winner);
        }

        SceneManager.LoadScene("WorldMapScene");
    }

    public int GetGold(int player)
    {
        if (player < 0 || player >= gold.Length) return 0;
        return gold[player];
    }

    void CancelPlacingUnit()
    {
        if (!isPlacingUnit) return;
        if (ghostUnit != null) Destroy(ghostUnit);
        isPlacingUnit = false;
        selectedUnitPrefab = null; // Töröljük a kiválasztást
    }

    // MÓDOSÍTOTT: Most már paraméterben kapja a prefabot és az árat
    void StartPlacingUnit(int player, GameObject prefabToPlace, int cost)
    {
        if (isPlacingUnit) return;

        if (gold[player] < cost)
        {
            Debug.Log($"Játékos {player + 1} nem engedheti meg magának ezt az egységet! (Ár: {cost})");
            return;
        }

        isPlacingUnit = true;
        activePlayer = player;

        // ÚJ: Eltároljuk, mit választottunk
        selectedUnitPrefab = prefabToPlace;
        selectedUnitCost = cost;

        // Ghost (szellem) unit létrehozása a választott prefabból
        ghostUnit = Instantiate(selectedUnitPrefab);
        SetTransparency(ghostUnit, 0.5f);

        // Kezdőpozíció
        Vector3 startPos = (activePlayer == 0)
            ? new Vector3(0f, 0.25f, -1f)
            : new Vector3(7f, 0.25f, -1f);

        ghostUnit.transform.position = startPos;
    }

    void FollowMouse()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        int layerMask = LayerMask.GetMask("LeftZone", "RightZone");

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, layerMask))
        {
            Vector3 mouseOffset = Camera.main.transform.right * 1f + Vector3.up * 0.25f;
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
        // Alap ellenőrzések
        if (!isPlacingUnit || tile.isOccupied) return;
        if (tile.CompareTag("Castle")) return;

        // Oldal ellenőrzés
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


        // Vásárlás a tárolt árral
        gold[activePlayer] -= selectedUnitCost;
        UpdateGoldUI();

        tile.isOccupied = true;

        // A KIVÁLASZTOTT unitot rakjuk le (Archer vagy Sword)
        Instantiate(selectedUnitPrefab, tile.transform.position + Vector3.up * 0.25f, Quaternion.identity);

        Destroy(ghostUnit);
        isPlacingUnit = false;
        ghostUnit = null;
        selectedUnitPrefab = null;
        Debug.Log($"Jatekos {activePlayer + 1} unitot helyezett le!");
    }

    void UpdateGoldUI()
    {
        if (leftGoldText != null) leftGoldText.text = $"{gold[0]}";
        if (rightGoldText != null) rightGoldText.text = $"{gold[1]}";
    }

    void SetTransparency(GameObject obj, float alpha)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            foreach (Material m in r.materials)
            {
                if (m.HasProperty("_Color"))
                {
                    Color c = m.color;
                    c.a = alpha;
                    m.color = c;
                    // Standard shader transparency hack (hogy átlátszó legyen a ghost)
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
}