using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

// Lightweight autosave helper that serializes world/gold/tile ownership into JSON.
// Usage: SaveManager.SaveWorldState(); // writes to Application.persistentDataPath/gamepath/saves/saveYYMMDD.json
// You can pass round and maxRounds if you have them available.
public static class SaveManager
{
    [Serializable]
    public class SaveData
    {
        public Metadata metadata;
        public GameState game_state;
    }

    [Serializable]
    public class Metadata
    {
        public string version;
        public string saved_at;
        public string game_id;
    }

    [Serializable]
    public class GameState
    {
        public int round;
        public int max_rounds;
        public PlayerState attacker;
        public PlayerState defender;
        public MapState map;
        public List<BattleHistoryEntry> battle_history;
    }

    [Serializable]
    public class PlayerState
    {
        public string name;
        public int points_remaining; // repurposed to store player gold if desired
        public int wins;
    }

    [Serializable]
    public class MapState
    {
        public int width;
        public int height;
        public List<TileEntry> tiles;
    }

    [Serializable]
    public class TileEntry
    {
        public int x;
        public int y;
        public string owner; // "attacker" / "defender" / "neutral"
    }

    [Serializable]
    public class BattleHistoryEntry
    {
        public int round;
        public string winner; // "attacker"/"defender"
    }

    // If a load is requested from the menu, the loaded DTO will be stored here
    // and consumed by WorldManager when the world scene loads.
    public static SaveData PendingLoad { get; private set; } = null;

    // Public entry point: call from your end-of-battle logic.
    // If you omit round/maxRounds they'll be saved as -1.
    // This method will write to: Application.persistentDataPath/gamepath/saves/saveYYMMDD.json
    public static bool SaveWorldState(int round = -1, int maxRounds = -1)
    {
        try
        {
            // build save container
            SaveData save = new SaveData();
            save.metadata = new Metadata
            {
                version = "1.0",
                saved_at = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss"),
                game_id = "frontline_save_" + DateTime.UtcNow.ToString("yyyyMMddHHmmss")
            };

            save.game_state = new GameState
            {
                round = round,
                max_rounds = maxRounds,
                attacker = new PlayerState { name = "Player1", points_remaining = 0, wins = 0 },
                defender = new PlayerState { name = "Player2", points_remaining = 0, wins = 0 },
                map = new MapState { width = 0, height = 0, tiles = new List<TileEntry>() },
                battle_history = new List<BattleHistoryEntry>()
            };

            // Get gold from WorldManager if available
            if (WorldManager.Instance != null)
            {
                try
                {
                    int[] gold = WorldManager.Instance.GetGold();
                    if (gold != null && gold.Length >= 2)
                    {
                        save.game_state.attacker.points_remaining = gold[0];
                        save.game_state.defender.points_remaining = gold[1];
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"SaveManager: failed to get gold from WorldManager: {e.Message}");
                }
            }

            // Gather all world tiles directly from scene (WorldHexTile objects)
            WorldHexTile[] allTiles = GameObject.FindObjectsByType<WorldHexTile>(FindObjectsSortMode.None);
            int maxX = 0, maxY = 0;
            if (allTiles != null && allTiles.Length > 0)
            {
                foreach (var t in allTiles)
                {
                    if (t == null) continue;
                    var entry = new TileEntry { x = t.hexX, y = t.hexZ, owner = "neutral" };

                    int leftLayer = LayerMask.NameToLayer("LeftZone");
                    int rightLayer = LayerMask.NameToLayer("RightZone");

                    if (t.gameObject != null)
                    {
                        if (t.gameObject.layer == leftLayer) entry.owner = "attacker";
                        else if (t.gameObject.layer == rightLayer) entry.owner = "defender";
                    }

                    save.game_state.map.tiles.Add(entry);

                    if (t.hexX > maxX) maxX = t.hexX;
                    if (t.hexZ > maxY) maxY = t.hexZ;
                }

                // set approximate width/height based on max coords (+1)
                save.game_state.map.width = maxX + 1;
                save.game_state.map.height = maxY + 1;
            }

            // Serialize with Unity's JsonUtility (fields must be public)
            string json = JsonUtility.ToJson(save, true);

            // Build target path: <persistentDataPath>/gamepath/saves/saveYYMMDD.json
            string dateSuffix = DateTime.Now.ToString("yyMMdd"); // local date when player pressed new game
            string relativeDir = Path.Combine("gamepath", "saves");
            string fileName = $"save{dateSuffix}.json";
            string dir = Path.Combine(Application.persistentDataPath, relativeDir);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            string path = Path.Combine(dir, fileName);
            File.WriteAllText(path, json);

            Debug.Log($"SaveManager: world saved to {path}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"SaveManager: failed to save world state: {ex.Message}");
            return false;
        }
    }

    // Loads the most recently modified save file (save*.json) into PendingLoad.
    // Returns true if a save was loaded successfully.
    public static bool LoadLatestSave()
    {
        try
        {
            string dir = Path.Combine(Application.persistentDataPath, "gamepath", "saves");
            if (!Directory.Exists(dir))
            {
                Debug.LogWarning($"SaveManager.LoadLatestSave: save folder does not exist: {dir}");
                return false;
            }

            string[] files = Directory.GetFiles(dir, "save*.json");
            if (files == null || files.Length == 0)
            {
                Debug.LogWarning("SaveManager.LoadLatestSave: no save files found.");
                return false;
            }

            // pick the most recently written file
            string latest = files.OrderByDescending(f => File.GetLastWriteTimeUtc(f)).First();
            string json = File.ReadAllText(latest);
            SaveData loaded = JsonUtility.FromJson<SaveData>(json);
            if (loaded == null)
            {
                Debug.LogWarning($"SaveManager.LoadLatestSave: failed to deserialize {latest}");
                return false;
            }

            PendingLoad = loaded;
            Debug.Log($"SaveManager: loaded latest save from {latest}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"SaveManager.LoadLatestSave failed: {ex.Message}");
            return false;
        }
    }

    // Opens the saves folder in the system file explorer. Creates the directory if missing.
    public static void OpenSavesFolder()
    {
        string dir = Path.Combine(Application.persistentDataPath, "gamepath", "saves");
        try
        {
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            // Use file:// URL to open folder across platforms
            string url = "file://" + dir.Replace("\\", "/");
            Application.OpenURL(url);
            Debug.Log($"SaveManager: opened saves folder: {dir}");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"SaveManager.OpenSavesFolder failed: {ex.Message}");
        }
    }

    // Consumes pending load (WorldManager should call this to retrieve and clear)
    public static SaveData ConsumePendingLoad()
    {
        var tmp = PendingLoad;
        PendingLoad = null;
        return tmp;
    }
}