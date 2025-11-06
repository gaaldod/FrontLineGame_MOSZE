using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    private void Awake() => Instance = this;

    [Header("Prefabs & UI")]
    public GameObject unitPrefab;
    public Button buyLeftButton;
    public Button buyRightButton;
    public TMP_Text leftGoldText;
    public TMP_Text rightGoldText;

    [Header("Economy Settings")]
    public int startingGold = 15;
    public int unitCost = 5;

    [Header("Player Colors")]
    public Color leftPlayerColor = Color.cyan;
    public Color rightPlayerColor = Color.red;

    private int[] gold = new int[2]; // [0] = bal, [1] = jobb
    private bool isPlacingUnit = false;
    private GameObject ghostUnit;
    private int activePlayer = 0;

    public int ActivePlayer => activePlayer;

    void Start()
    {
        // Kezdő arany
        gold[0] = startingGold;
        gold[1] = startingGold;
        UpdateGoldUI();

        buyLeftButton.onClick.AddListener(() => StartPlacingUnit(0));
        buyRightButton.onClick.AddListener(() => StartPlacingUnit(1));
    }

    void Update()
    {
        if (isPlacingUnit && ghostUnit != null)
        {
            FollowMouse();

            // SPACE: lehelyezés megszakítása
            if (Input.GetKeyDown(KeyCode.Space))
            {
                CancelPlacingUnit();
            }
        }
    }

    void CancelPlacingUnit()
    {
        if (!isPlacingUnit) return;

        Debug.Log("❌ Unit lerakás megszakítva.");

        if (ghostUnit != null)
        {
            Destroy(ghostUnit);
            ghostUnit = null;
        }

        isPlacingUnit = false;
    }

    void StartPlacingUnit(int player)
    {
        if (isPlacingUnit) return;

        // 💰 Gold ellenőrzés
        if (gold[player] < unitCost)
        {
            Debug.Log($"❌ Játékos {player + 1} nem engedheti meg magának a unitot! ({gold[player]} / {unitCost})");
            return;
        }

        isPlacingUnit = true;
        activePlayer = player;

        // 👻 Ghost létrehozása
        ghostUnit = Instantiate(unitPrefab);
        SetTransparency(ghostUnit, 0.5f);

        // 🎯 Alap spawnpozíció
        Vector3 startPos = activePlayer == 0
            ? new Vector3(-2f, 0.15f, 0f)
            : new Vector3(10f, 0.15f, 0f);

        ghostUnit.transform.position = startPos;

        Debug.Log($"🎯 Játékos {activePlayer + 1} elkezdett unitot elhelyezni ({gold[player]} gold maradt).");
    }

    void FollowMouse()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        int layerMask = LayerMask.GetMask("LeftZone", "RightZone");

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, layerMask))
        {
            // 📍 A ghost mindig az egér MELLÉ kerül
            Vector3 mouseOffset = Camera.main.transform.right * 1f + Vector3.up * 0.1f;
            Vector3 targetPos = hit.point + mouseOffset;

            ghostUnit.transform.position = Vector3.Lerp(
                ghostUnit.transform.position,
                targetPos,
                Time.deltaTime * 20f
            );
        }
        else
        {
            // Ha nincs találat, marad a sarokban
            Vector3 idlePos = activePlayer == 0
                ? new Vector3(1f, 0.15f, -1f)
                : new Vector3(6f, 0.15f, -1f);

            ghostUnit.transform.position = Vector3.Lerp(
                ghostUnit.transform.position,
                idlePos,
                Time.deltaTime * 10f
            );
        }
    }

    public void TryPlaceUnit(HexTile tile)
    {
        if (!isPlacingUnit || tile.isOccupied) return;
        if (tile.CompareTag("Castle")) return; // ne lehessen kastélyra rakni

        // Zóna ellenőrzés
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

        // Gold levonás
        gold[activePlayer] -= unitCost;
        UpdateGoldUI();

        tile.isOccupied = true;

        Instantiate(unitPrefab, tile.transform.position + Vector3.up * 0.1f, Quaternion.identity);
        Destroy(ghostUnit);
        ghostUnit = null;
        isPlacingUnit = false;

        Debug.Log($"Játékos {activePlayer + 1} unitot helyezett le! ({gold[activePlayer]} gold maradt)");
    }

    void UpdateGoldUI()
    {
        leftGoldText.text = $"Arany: {gold[0]}";
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
