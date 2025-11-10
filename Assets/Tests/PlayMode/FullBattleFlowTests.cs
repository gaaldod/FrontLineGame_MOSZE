using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using System.Collections.Generic;

public class FullBattleFlowTests
{
    private GameObject wmObj;
    private WorldManager wm;
    private GameObject castleTileObj;
    private WorldHexTile castleTile;
    private GameObject normalTileObj;
    private WorldHexTile normalTile;

    [SetUp]
    public void Setup()
    {
        // WorldManager létrehozása
        wmObj = new GameObject("WorldManager");
        wm = wmObj.AddComponent<WorldManager>();
        WorldManager.Instance = wm;

        // Castle tile létrehozása
        castleTileObj = new GameObject("CastleTile");
        castleTile = castleTileObj.AddComponent<WorldHexTile>();
        castleTile.hexX = 0;
        castleTile.hexZ = 0;
        castleTile.isCastleTile = true;

        // Normál tile létrehozása
        normalTileObj = new GameObject("NormalTile");
        normalTile = normalTileObj.AddComponent<WorldHexTile>();
        normalTile.hexX = 1;
        normalTile.hexZ = 1;
        normalTile.isCastleTile = false;

        // Minden tile hozzáadása a WorldManager-hez
        typeof(WorldManager).GetField("allTiles", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .SetValue(wm, new[] { castleTile, normalTile });
    }

    [TearDown]
    public void Teardown()
    {
        Object.DestroyImmediate(castleTileObj);
        Object.DestroyImmediate(normalTileObj);
        Object.DestroyImmediate(wmObj);
        WorldManager.Instance = null;
    }

    [UnityTest]
    public IEnumerator BattleFlow_WinnerLeft_PlayerWinsCastle_GameOverTriggered()
    {
        // Bal játékos nyer a kastélynál
        wm.RecordBattleStart(0, 0);
        wm.RecordBattleResult(0);

        // Capture Debug.Log-ot
        LogAssert.Expect(LogType.Log, "🏁 Game Over! A bal játékos elfoglalta a kastélyt és megnyerte a játékot!");

        // Coroutine futtatása
        yield return wm.StartCoroutine("ApplyPendingAfterWorldLoad");

        // Ellenőrzés: layer frissült
        Assert.AreEqual(LayerMask.NameToLayer("LeftZone"), castleTile.gameObject.layer);
    }

    [UnityTest]
    public IEnumerator BattleFlow_NormalTile_OwnershipTransferred()
    {
        // Bal játékos nyer egy normál mezőt
        wm.RecordBattleStart(1, 1);
        wm.RecordBattleResult(0);

        // Engedélyezzük az ownership logot
        LogAssert.Expect(LogType.Log, "✅ Tile (1,1) ownership frissítve: 0");

        yield return wm.StartCoroutine("ApplyPendingAfterWorldLoad");

        // Ellenőrzés: tileOwners frissült
        var tileOwners = (Dictionary<(int, int), int>)typeof(WorldManager)
            .GetField("tileOwners", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .GetValue(wm);

        Assert.IsTrue(tileOwners.ContainsKey((1, 1)));
        Assert.AreEqual(0, tileOwners[(1, 1)]);

        // GameOver log NEM jött, mert nem kastély
        LogAssert.NoUnexpectedReceived();
    }
}
