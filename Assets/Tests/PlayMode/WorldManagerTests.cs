using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.TestTools;

public class WorldManagerTests
{
    private GameObject managerObj;
    private WorldManager manager;

    [SetUp]
    public void Setup()
    {
        managerObj = new GameObject("WorldManager");
        manager = managerObj.AddComponent<WorldManager>();
        WorldManager.Instance = manager;
    }

    [TearDown]
    public void Teardown()
    {
        Object.DestroyImmediate(managerObj);
        WorldManager.Instance = null;
    }

    [Test]
    public void InitGold_CorrectlyInitialized()
    {
        manager.GetType().GetMethod("Start", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            .Invoke(manager, null);

        var gold = manager.GetGold();
        Assert.AreEqual(manager.startingGold, gold[0]);
        Assert.AreEqual(manager.startingGold, gold[1]);
    }

    [Test]
    public void RecordBattleStart_SetsPendingCorrectly()
    {
        manager.RecordBattleStart(3, 5);
        Assert.IsTrue((bool)manager.GetType().GetField("hasPendingBattle", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(manager));
    }

    [Test]
    public void RecordBattleResult_StoresWinnerCorrectly()
    {
        manager.RecordBattleResult(1);
        int result = (int)manager.GetType().GetField("pendingBattleWinner", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(manager);
        Assert.AreEqual(1, result);
    }

    [UnityTest]
    public IEnumerator ApplyPendingAfterWorldLoad_UpdatesTileOwnership()
    {
        var tileObj = new GameObject("Tile");
        var tile = tileObj.AddComponent<WorldHexTile>();
        tile.hexX = 2;
        tile.hexZ = 3;
        tile.isCastleTile = false;

        typeof(WorldManager).GetField("allTiles", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .SetValue(manager, new[] { tile });

        manager.RecordBattleStart(2, 3);
        manager.RecordBattleResult(0);

        yield return manager.StartCoroutine("ApplyPendingAfterWorldLoad");

        var tileOwners = (System.Collections.Generic.Dictionary<(int, int), int>)manager.GetType()
            .GetField("tileOwners", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .GetValue(manager);

        Assert.IsTrue(tileOwners.ContainsKey((2, 3)));
        Assert.AreEqual(0, tileOwners[(2, 3)]);
    }

    [UnityTest]
    public IEnumerator CastleTileCaptured_TriggersGameOver()
    {
        var tileObj = new GameObject("CastleTile");
        var tile = tileObj.AddComponent<WorldHexTile>();
        tile.hexX = 7;
        tile.hexZ = 7;
        tile.isCastleTile = true;

        typeof(WorldManager).GetField("allTiles", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .SetValue(manager, new[] { tile });

        manager.RecordBattleStart(7, 7);
        manager.RecordBattleResult(0);

        yield return manager.StartCoroutine("ApplyPendingAfterWorldLoad");

        LogAssert.Expect(LogType.Log, "🏁 Game Over! A bal játékos elfoglalta a kastélyt és megnyerte a játékot!");
    }
}
