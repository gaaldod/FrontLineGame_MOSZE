using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class WorldManager : MonoBehaviour
{
    public static WorldManager Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [Header("UI Elemei")]
    public TMP_Text leftGoldText;
    public TMP_Text rightGoldText;

    [Header("Játék Beállítások")]
    public int startingGold = 15;

    [Header("Játékos Színek")]
    public Color leftPlayerColor = Color.red;
    public Color rightPlayerColor = Color.cyan;

    private int[] gold = new int[2];
    private bool goldInitialized = false;

    private Dictionary<(int, int), int> tileOwners = new();
    private WorldHexTile[] allTiles;

    private bool hasPendingBattle = false;
    private int pendingBattleHexX = -999;
    private int pendingBattleHexZ = -999;
    private int pendingBattleWinner = -1;

    void Start()
    {
        if (!goldInitialized)
        {
            gold[0] = startingGold;
            gold[1] = startingGold;
            goldInitialized = true;
        }

        TryFindUI();
        UpdateGoldUI();
        TryRefreshTiles();
    }

    public int[] GetGold() => gold;

    public void SetGold(int[] newGold)
    {
        gold = newGold;
        UpdateGoldUI();
    }

    public void RecordBattleStart(int hexX, int hexZ)
    {
        pendingBattleHexX = hexX;
        pendingBattleHexZ = hexZ;
        hasPendingBattle = true;
        pendingBattleWinner = -1;
    }

    public void RecordBattleResult(int winner)
    {
        pendingBattleWinner = winner;
        hasPendingBattle = true;
    }

    public bool IsTileClickable(WorldHexTile tile) => IsClickableTile(tile);

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "WorldMapScene")
        {
            StartCoroutine(ApplyPendingAfterWorldLoad());
        }
    }

    private bool IsClickableTile(WorldHexTile tile)
    {
        if (tile == null || tile.gameObject == null) return false;
        if (tile.gameObject.layer != LayerMask.NameToLayer("RightZone")) return false;

        if (allTiles == null || allTiles.Length == 0)
            TryRefreshTiles();

        foreach (var other in allTiles)
        {
            if (other == null || other.gameObject == null) continue;
            if (other.gameObject.layer != LayerMask.NameToLayer("LeftZone")) continue;
            if (IsNeighbor(tile, other)) return true;
        }

        return false;
    }

    private bool IsNeighbor(WorldHexTile a, WorldHexTile b)
    {
        int dx = b.hexX - a.hexX;
        int dz = b.hexZ - a.hexZ;
        bool isOddColumn = (a.hexX % 2 != 0);

        if (isOddColumn)
        {
            return (dx == -2 && dz == 0) ||
                   (dx == -1 && dz == 0) ||
                   (dx == 1 && dz == 0) ||
                   (dx == 2 && dz == 0) ||
                   (dx == -1 && dz == 1) ||
                   (dx == 1 && dz == 1);
        }
        else
        {
            return (dx == -2 && dz == 0) ||
                   (dx == -1 && dz == 0) ||
                   (dx == 1 && dz == 0) ||
                   (dx == 2 && dz == 0) ||
                   (dx == -1 && dz == -1) ||
                   (dx == 1 && dz == -1);
        }
    }

    public void TryRefreshTiles()
    {
        allTiles = GameObject.FindObjectsByType<WorldHexTile>(FindObjectsSortMode.None);
    }

    // apply pending load (from MainMenu) and pending battle result, then refresh UI
    private IEnumerator ApplyPendingAfterWorldLoad()
    {
        yield return null;
        yield return null;

        TryFindUI();
        TryRefreshTiles();

        // If there's a pending save loaded from menu, apply it first
        var pending = SaveManager.ConsumePendingLoad();
        if (pending != null)
        {
            // apply gold
            try
            {
                int left = pending.game_state?.attacker?.points_remaining ?? gold[0];
                int right = pending.game_state?.defender?.points_remaining ?? gold[1];
                SetGold(new int[] { left, right });
            }
            catch { /* swallow */ }

            // apply tile ownership mapping
            try
            {
                tileOwners.Clear();
                if (pending.game_state?.map?.tiles != null)
                {
                    foreach (var te in pending.game_state.map.tiles)
                    {
                        if (te == null) continue;
                        int owner = -1;
                        if (!string.IsNullOrEmpty(te.owner))
                        {
                            if (te.owner.Equals("attacker", System.StringComparison.OrdinalIgnoreCase)) owner = 0;
                            else if (te.owner.Equals("defender", System.StringComparison.OrdinalIgnoreCase)) owner = 1;
                        }

                        if (owner >= 0)
                            tileOwners[(te.x, te.y)] = owner;
                    }
                }
            }
            catch { /* swallow */ }

            Debug.Log("WorldManager: applied pending save data from SaveManager.");
        }

        foreach (var t in allTiles)
        {
            if (t == null) continue;
            var key = (t.hexX, t.hexZ);
            if (tileOwners.TryGetValue(key, out int owner))
            {
                if (owner == 0) t.gameObject.layer = LayerMask.NameToLayer("LeftZone");
                else if (owner == 1) t.gameObject.layer = LayerMask.NameToLayer("RightZone");
            }
        }

        //csak akkor fut le, ha tényleg volt csata
        if (hasPendingBattle && (pendingBattleWinner == 0 || pendingBattleWinner == 1))
        {
            WorldHexTile target = null;
            foreach (var t in allTiles)
            {
                if (t == null) continue;
                if (t.hexX == pendingBattleHexX && t.hexZ == pendingBattleHexZ)
                {
                    target = t;
                    break;
                }
            }

            if (target != null)
            {
                int layer = (pendingBattleWinner == 0)
                    ? LayerMask.NameToLayer("LeftZone")
                    : LayerMask.NameToLayer("RightZone");

                target.gameObject.layer = layer;
                target.isOccupied = false;
                target.ResetColor();
                tileOwners[(target.hexX, target.hexZ)] = pendingBattleWinner;

                Debug.Log($"✅ Tile ({target.hexX},{target.hexZ}) ownership frissítve: {pendingBattleWinner}");

                // Autosave the world state after applying the battle result
                try
                {
                    bool saved = SaveManager.SaveWorldState();
                    if (saved) Debug.Log("SaveManager: autosave created after applying battle result.");
                    else Debug.LogWarning("SaveManager: autosave reported failure.");
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"SaveManager: exception while autosaving: {ex.Message}");
                }

                // Ha ez kastély és a bal játékos nyert -> Game Over
                if (target.isCastleTile && pendingBattleWinner == 0)
                {
                    Debug.Log("🏁 Game Over! A bal játékos elfoglalta a kastélyt és megnyerte a játékot!");
                    StartCoroutine(HandleGameOver());
                    yield break;
                }
            }
        }

        UpdateGoldUI();
        hasPendingBattle = false;
        pendingBattleHexX = -999;
        pendingBattleHexZ = -999;
        pendingBattleWinner = -1;
    }

    private IEnumerator HandleGameOver()
    {
        yield return new WaitForSeconds(2f);

        // minden adat törlése
        tileOwners.Clear();
        goldInitialized = false;
        hasPendingBattle = false;

        // WorldManager leiratkozik és megsemmisül
        SceneManager.sceneLoaded -= OnSceneLoaded;
        Instance = null;
        Destroy(gameObject);

        // főmenü betöltése
        SceneManager.LoadScene("MainMenu");
    }

    private void TryFindUI()
    {
        if (leftGoldText == null)
            leftGoldText = GameObject.Find("GoldTextLeft")?.GetComponent<TMP_Text>();
        if (rightGoldText == null)
            rightGoldText = GameObject.Find("GoldTextRight")?.GetComponent<TMP_Text>();
    }

    public void UpdateGoldUI()
    {
        if (leftGoldText != null) leftGoldText.text = $"Arany: {gold[0]}";
        if (rightGoldText != null) rightGoldText.text = $"Arany: {gold[1]}";
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
