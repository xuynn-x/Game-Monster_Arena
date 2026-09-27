using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

public static class LocalAccountService
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    // Explicit validation runs use separate keys and never read/write the user's accounts.
    internal static string ValidationStorageSuffix = string.Empty;
    private static string DatabaseKey => "MonsterArena.LocalAccounts.v1" + ValidationStorageSuffix;
    private static string CurrentAccountKey => "MonsterArena.CurrentAccountId.v1" + ValidationStorageSuffix;
#else
    private const string DatabaseKey = "MonsterArena.LocalAccounts.v1";
    private const string CurrentAccountKey = "MonsterArena.CurrentAccountId.v1";
#endif
    private const string DefaultAvatarId = "avatar_pink_trainer_portrait";
    private const string LegacyAvatarId = "avatar_default";
    private const int PasswordIterations = 100_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    public static bool Register(
        string username,
        string password,
        string displayName,
        out string errorMessage)
    {
        username = username?.Trim();
        displayName = displayName?.Trim();

        if (!ValidateRegistration(
                username,
                password,
                displayName,
                out errorMessage))
        {
            return false;
        }

        LocalAccountDatabase database = LoadDatabase();
        string normalizedUsername = NormalizeUsername(username);

        if (FindByUsername(database, normalizedUsername) != null)
        {
            errorMessage = "Tên đăng nhập đã tồn tại.";
            return false;
        }

        CreatePasswordHash(
            password,
            out string passwordHash,
            out string passwordSalt
        );

        string nowUtc = DateTime.UtcNow.ToString("O");
        LocalAccountData account = new()
        {
            accountId = Guid.NewGuid().ToString("N"),
            username = username,
            normalizedUsername = normalizedUsername,
            displayName = displayName,
            passwordHash = passwordHash,
            passwordSalt = passwordSalt,
            avatarId = DefaultAvatarId,
            ownedMonsterIds = new List<string>(),
            selectedMonsterId = string.Empty,
            matchHistory = new List<LocalMatchHistoryEntry>(),
            createdAtUtc = nowUtc,
            lastLoginAtUtc = nowUtc
        };

        database.accounts.Add(account);
        SaveDatabase(database);
        SetCurrentAccount(account.accountId);

        errorMessage = string.Empty;
        return true;
    }

    public static bool Login(
        string username,
        string password,
        out string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrEmpty(password))
        {
            errorMessage = "Hãy nhập tên đăng nhập và mật khẩu.";
            return false;
        }

        LocalAccountDatabase database = LoadDatabase();
        LocalAccountData account = FindByUsername(
            database,
            NormalizeUsername(username)
        );

        if (account == null || !VerifyPassword(account, password))
        {
            errorMessage = "Tên đăng nhập hoặc mật khẩu không đúng.";
            return false;
        }

        account.lastLoginAtUtc = DateTime.UtcNow.ToString("O");
        SaveDatabase(database);
        SetCurrentAccount(account.accountId);

        errorMessage = string.Empty;
        return true;
    }

    public static void Logout()
    {
        PlayerPrefs.DeleteKey(CurrentAccountKey);
        PlayerPrefs.Save();
    }

    public static bool TryGetCurrentAccount(out LocalAccountData account)
    {
        account = null;

        string accountId = PlayerPrefs.GetString(
            CurrentAccountKey,
            string.Empty
        );

        if (string.IsNullOrEmpty(accountId))
        {
            return false;
        }

        LocalAccountDatabase database = LoadDatabase();
        account = database.accounts.Find(item => item.accountId == accountId);

        if (account != null)
        {
            if (string.IsNullOrEmpty(account.avatarId) ||
                account.avatarId == LegacyAvatarId)
            {
                account.avatarId = DefaultAvatarId;
                SaveDatabase(database);
            }

            return true;
        }

        Logout();
        return false;
    }

    public static bool UpdateProfile(
        string displayName,
        string avatarId,
        out string errorMessage)
    {
        displayName = displayName?.Trim();
        avatarId = avatarId?.Trim();

        if (string.IsNullOrWhiteSpace(displayName))
        {
            errorMessage = "Tên người chơi không được để trống.";
            return false;
        }

        LocalAccountDatabase database = LoadDatabase();
        LocalAccountData account = FindCurrentAccount(database);

        if (account == null)
        {
            errorMessage = "Bạn chưa đăng nhập.";
            return false;
        }

        account.displayName = displayName;
        account.avatarId = string.IsNullOrEmpty(avatarId)
            ? DefaultAvatarId
            : avatarId;

        SaveDatabase(database);
        errorMessage = string.Empty;
        return true;
    }

    public static bool AddOwnedMonster(
        string monsterId,
        out string errorMessage)
    {
        monsterId = monsterId?.Trim();

        if (string.IsNullOrEmpty(monsterId))
        {
            errorMessage = "Mã quái không hợp lệ.";
            return false;
        }

        LocalAccountDatabase database = LoadDatabase();
        LocalAccountData account = FindCurrentAccount(database);

        if (account == null)
        {
            errorMessage = "Bạn chưa đăng nhập.";
            return false;
        }

        if (!account.ownedMonsterIds.Contains(monsterId))
        {
            account.ownedMonsterIds.Add(monsterId);
        }

        if (string.IsNullOrEmpty(account.selectedMonsterId))
        {
            account.selectedMonsterId = monsterId;
        }

        SaveDatabase(database);
        errorMessage = string.Empty;
        return true;
    }

    public static bool SelectMonster(
        string monsterId,
        out string errorMessage)
    {
        LocalAccountDatabase database = LoadDatabase();
        LocalAccountData account = FindCurrentAccount(database);

        if (account == null)
        {
            errorMessage = "Bạn chưa đăng nhập.";
            return false;
        }

        if (string.IsNullOrEmpty(monsterId) ||
            !account.ownedMonsterIds.Contains(monsterId))
        {
            errorMessage = "Tài khoản chưa sở hữu quái này.";
            return false;
        }

        account.selectedMonsterId = monsterId;
        SaveDatabase(database);
        errorMessage = string.Empty;
        return true;
    }

    public static bool AddMatchHistory(
        LocalMatchHistoryEntry match,
        out string errorMessage)
    {
        if (match == null)
        {
            errorMessage = "Kết quả trận không hợp lệ.";
            return false;
        }

        LocalAccountDatabase database = LoadDatabase();
        LocalAccountData account = FindCurrentAccount(database);

        if (account == null)
        {
            errorMessage = "Bạn chưa đăng nhập.";
            return false;
        }

        if (string.IsNullOrEmpty(match.matchId))
        {
            match.matchId = Guid.NewGuid().ToString("N");
        }

        if (string.IsNullOrEmpty(match.playedAtUtc))
        {
            match.playedAtUtc = DateTime.UtcNow.ToString("O");
        }

        account.matchHistory.Add(match);
        SaveDatabase(database);
        errorMessage = string.Empty;
        return true;
    }

    private static bool ValidateRegistration(
        string username,
        string password,
        string displayName,
        out string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(username) ||
            username.Length < 3 ||
            username.Length > 20)
        {
            errorMessage = "Tên đăng nhập phải có từ 3 đến 20 ký tự.";
            return false;
        }

        foreach (char character in username)
        {
            if (!char.IsLetterOrDigit(character) &&
                character != '_' &&
                character != '.')
            {
                errorMessage =
                    "Tên đăng nhập chỉ dùng chữ, số, dấu chấm hoặc gạch dưới.";
                return false;
            }
        }

        if (string.IsNullOrEmpty(password) || password.Length < 6)
        {
            errorMessage = "Mật khẩu phải có ít nhất 6 ký tự.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(displayName) ||
            displayName.Length > 24)
        {
            errorMessage = "Tên người chơi phải có từ 1 đến 24 ký tự.";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }

    private static string NormalizeUsername(string username)
    {
        return username.Trim().ToLowerInvariant();
    }

    private static LocalAccountData FindByUsername(
        LocalAccountDatabase database,
        string normalizedUsername)
    {
        return database.accounts.Find(
            account => account.normalizedUsername == normalizedUsername
        );
    }

    private static LocalAccountData FindCurrentAccount(
        LocalAccountDatabase database)
    {
        string accountId = PlayerPrefs.GetString(
            CurrentAccountKey,
            string.Empty
        );

        return database.accounts.Find(
            account => account.accountId == accountId
        );
    }

    private static LocalAccountDatabase LoadDatabase()
    {
        string json = PlayerPrefs.GetString(DatabaseKey, string.Empty);

        if (string.IsNullOrWhiteSpace(json))
        {
            return new LocalAccountDatabase();
        }

        try
        {
            LocalAccountDatabase database =
                JsonUtility.FromJson<LocalAccountDatabase>(json);

            if (database == null)
            {
                return new LocalAccountDatabase();
            }

            database.accounts ??= new List<LocalAccountData>();

            foreach (LocalAccountData account in database.accounts)
            {
                account.ownedMonsterIds ??= new List<string>();
                account.matchHistory ??= new List<LocalMatchHistoryEntry>();
                NormalizeTeam(account);
            }

            return database;
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"Không đọc được dữ liệu tài khoản local: {exception.Message}"
            );
            return new LocalAccountDatabase();
        }
    }

    private static void SaveDatabase(LocalAccountDatabase database)
    {
        string json = JsonUtility.ToJson(database);
        PlayerPrefs.SetString(DatabaseKey, json);
        PlayerPrefs.Save();
    }

    public const int MaxTeamSize = 5;

    // Migrate old accounts once. An intentionally empty team stays empty.
    public static void NormalizeTeam(LocalAccountData account)
    {
        account.ownedMonsterIds ??= new List<string>();
        account.teamMonsterIds ??= new List<string>();
        if (account.teamSchemaVersion == 0)
        {
            if (account.teamMonsterIds.Count == 0 &&
                !string.IsNullOrEmpty(account.selectedMonsterId) &&
                account.ownedMonsterIds.Contains(account.selectedMonsterId))
                account.teamMonsterIds.Add(account.selectedMonsterId);
            account.teamSchemaVersion = 1;
        }
        var valid = new List<string>();
        foreach (string id in account.teamMonsterIds)
            if (!string.IsNullOrEmpty(id) && account.ownedMonsterIds.Contains(id) &&
                !valid.Contains(id) && valid.Count < MaxTeamSize) valid.Add(id);
        account.teamMonsterIds = valid;
    }

    public static bool ValidateTeam(LocalAccountData account, IList<string> ids, out string error)
    {
        if (account == null) { error = "Bạn chưa đăng nhập."; return false; }
        if (ids == null || ids.Count > MaxTeamSize)
        { error = "Đội hình chỉ được có tối đa 5 quái."; return false; }
        var seen = new HashSet<string>();
        foreach (string id in ids)
        {
            if (string.IsNullOrEmpty(id) || account.ownedMonsterIds == null || !account.ownedMonsterIds.Contains(id))
            { error = "Chỉ được chọn quái bạn đang sở hữu."; return false; }
            if (!seen.Add(id)) { error = "Một quái không thể xuất hiện hai lần trong đội."; return false; }
        }
        error = string.Empty;
        return true;
    }

    public static bool SaveTeam(IList<string> ids, out string error)
    {
        LocalAccountDatabase database = LoadDatabase();
        LocalAccountData account = FindCurrentAccount(database);
        if (!ValidateTeam(account, ids, out error)) return false;
        account.teamMonsterIds = new List<string>(ids);
        account.teamSchemaVersion = 1;
        SaveDatabase(database);
        return true;
    }

    private static void SetCurrentAccount(string accountId)
    {
        PlayerPrefs.SetString(CurrentAccountKey, accountId);
        PlayerPrefs.Save();
    }

    private static void CreatePasswordHash(
        string password,
        out string passwordHash,
        out string passwordSalt)
    {
        byte[] salt = new byte[SaltSize];

        using (RandomNumberGenerator random = RandomNumberGenerator.Create())
        {
            random.GetBytes(salt);
        }

        using Rfc2898DeriveBytes deriveBytes = new(
            password,
            salt,
            PasswordIterations,
            HashAlgorithmName.SHA256
        );

        passwordHash = Convert.ToBase64String(
            deriveBytes.GetBytes(HashSize)
        );
        passwordSalt = Convert.ToBase64String(salt);
    }

    private static bool VerifyPassword(
        LocalAccountData account,
        string password)
    {
        try
        {
            byte[] salt = Convert.FromBase64String(account.passwordSalt);
            byte[] expectedHash = Convert.FromBase64String(
                account.passwordHash
            );

            using Rfc2898DeriveBytes deriveBytes = new(
                password,
                salt,
                PasswordIterations,
                HashAlgorithmName.SHA256
            );

            byte[] actualHash = deriveBytes.GetBytes(expectedHash.Length);
            return FixedTimeEquals(actualHash, expectedHash);
        }
        catch (FormatException)
        {
            return false;
        }
        catch (CryptographicException)
        {
            return false;
        }
    }

    private static bool FixedTimeEquals(byte[] first, byte[] second)
    {
        if (first == null || second == null || first.Length != second.Length)
        {
            return false;
        }

        int difference = 0;

        for (int index = 0; index < first.Length; index++)
        {
            difference |= first[index] ^ second[index];
        }

        return difference == 0;
    }
}
