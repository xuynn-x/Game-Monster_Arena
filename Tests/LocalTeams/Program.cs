using System;
using System.Collections.Generic;
using System.Text.Json;

internal static class Program
{
    private static int passed;
    private static void Check(bool result, string name)
    {
        if (!result) throw new Exception("FAIL: " + name);
        passed++;
        Console.WriteLine("PASS: " + name);
    }

    public static void Main()
    {
        // In-memory PlayerPrefs only: never reads or modifies real Unity account data.
        var legacy = new LocalAccountData { selectedMonsterId = "a", ownedMonsterIds = new() { "a" } };
        LocalAccountService.NormalizeTeam(legacy);
        Check(legacy.teamMonsterIds.Count == 1 && legacy.teamMonsterIds[0] == "a", "Migrate legacy companion");
        legacy.teamMonsterIds.Clear();
        LocalAccountService.NormalizeTeam(legacy);
        Check(legacy.teamMonsterIds.Count == 0, "Intentionally empty team stays empty");
        legacy.teamMonsterIds = new() { "a", "a", "missing", null };
        LocalAccountService.NormalizeTeam(legacy);
        Check(legacy.teamMonsterIds.Count == 1, "Normalize duplicate, missing and null entries");
        Check(LocalAccountService.Register("teamtest", "test-password", "Team Test", out _), "Create isolated test account");
        for (int i = 0; i < 6; i++) LocalAccountService.AddOwnedMonster("m" + i, out _);
        Check(LocalAccountService.SaveTeam(new[] { "m0", "m1", "m2", "m3", "m4" }, out _), "Save five owned monsters");
        Check(!LocalAccountService.SaveTeam(new[] { "m0", "m1", "m2", "m3", "m4", "m5" }, out _), "Reject six monsters");
        Check(!LocalAccountService.SaveTeam(new[] { "m0", "m0" }, out _), "Reject duplicates");
        Check(!LocalAccountService.SaveTeam(new[] { "not-owned" }, out _), "Reject unowned monsters");
        Check(!LocalAccountService.SaveTeam(null, out _), "Reject null input");
        LocalAccountService.TryGetCurrentAccount(out var account);
        Check(account.teamMonsterIds.Count == 5, "Failed saves preserve stored team");
        Check(LocalAccountService.SaveTeam(new[] { "m4", "m0" }, out _), "Save reordered and reduced team");
        LocalAccountService.Logout();
        Check(!LocalAccountService.SaveTeam(new[] { "m0" }, out _), "Reject save without account");
        LocalAccountService.Login("teamtest", "test-password", out _);
        LocalAccountService.TryGetCurrentAccount(out account);
        Check(string.Join(",", account.teamMonsterIds) == "m4,m0", "Order survives logout and login");
        LocalAccountService.SaveTeam(Array.Empty<string>(), out _);
        LocalAccountService.TryGetCurrentAccount(out account);
        Check(account.teamMonsterIds.Count == 0 && account.ownedMonsterIds.Count == 6, "Empty team persists without deleting ownership");
        LocalAccountService.Register("secondtest", "test-password", "Second", out _);
        LocalAccountService.TryGetCurrentAccount(out account);
        Check(account.teamMonsterIds.Count == 0 && account.ownedMonsterIds.Count == 0, "Accounts remain isolated");
        Check(!BattleEntryRules.Validate(null, out _), "Battle rejects missing account");
        Check(!BattleEntryRules.Validate(account, out _), "Battle rejects empty team");
        LocalAccountService.AddOwnedMonster(MonsterCatalog.StarterShadowFoxId, out _);
        LocalAccountService.SaveTeam(new[] { MonsterCatalog.StarterShadowFoxId }, out _);
        LocalAccountService.TryGetCurrentAccount(out account);
        Check(BattleEntryRules.Validate(account, out _), "Battle accepts owned Shadow Fox team");
        account.teamMonsterIds.Add(MonsterCatalog.StarterShadowFoxId);
        Check(!BattleEntryRules.Validate(account, out _), "Battle rejects duplicate team");
        account.teamMonsterIds = new() { "unknown" };
        Check(!BattleEntryRules.Validate(account, out _), "Battle rejects unowned monster");
        account.ownedMonsterIds.Add("unknown");
        Check(!BattleEntryRules.Validate(account, out _), "Battle rejects unsupported combat monster");
        account.teamMonsterIds.Add(MonsterCatalog.StarterShadowFoxId);
        Check(!BattleEntryRules.Validate(account, out _), "Battle rejects unsupported multi-monster combat");
        Console.WriteLine($"{passed} tests passed.");
    }
}

namespace UnityEngine
{
    public static class PlayerPrefs
    {
        private static readonly Dictionary<string, string> Data = new();
        public static string GetString(string key, string fallback) => Data.TryGetValue(key, out var value) ? value : fallback;
        public static void SetString(string key, string value) => Data[key] = value;
        public static void DeleteKey(string key) => Data.Remove(key);
        public static void Save() { }
    }
    public static class JsonUtility
    {
        private static readonly JsonSerializerOptions Options = new() { IncludeFields = true };
        public static T FromJson<T>(string json) => JsonSerializer.Deserialize<T>(json, Options);
        public static string ToJson(object value) => JsonSerializer.Serialize(value, Options);
    }
    public static class Debug
    {
        public static void LogError(object message) => throw new Exception(message.ToString());
    }
}
