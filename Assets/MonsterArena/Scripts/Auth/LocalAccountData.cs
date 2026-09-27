using System;
using System.Collections.Generic;

[Serializable]
public class LocalAccountDatabase
{
    public int schemaVersion = 1;
    public List<LocalAccountData> accounts = new();
}

[Serializable]
public class LocalAccountData
{
    public string accountId;
    public string username;
    public string normalizedUsername;
    public string displayName;
    public int level = 1;
    public int experience;
    public string passwordHash;
    public string passwordSalt;
    public string avatarId;
    public List<string> ownedMonsterIds = new();
    public string selectedMonsterId;
    public int teamSchemaVersion;
    public List<string> teamMonsterIds = new();
    public List<LocalMatchHistoryEntry> matchHistory = new();
    public string createdAtUtc;
    public string lastLoginAtUtc;
}

[Serializable]
public class LocalMatchHistoryEntry
{
    public string matchId;
    public string opponentName;
    public string playerMonsterId;
    public string opponentMonsterId;
    public bool playerWon;
    public string playedAtUtc;
}
