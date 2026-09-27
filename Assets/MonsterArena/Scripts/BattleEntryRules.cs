public static class BattleEntryRules
{
    public static bool Validate(LocalAccountData account, out string error)
    {
        if (!LocalAccountService.ValidateTeam(account, account?.teamMonsterIds, out error)) return false;
        if (account.teamMonsterIds.Count == 0)
        {
            error = "Đội đang trống. Hãy mở TEAM và thêm Shadow Fox trước khi tìm trận.";
            return false;
        }
        if (account.teamMonsterIds.Count != 1 || account.teamMonsterIds[0] != MonsterCatalog.StarterShadowFoxId)
        {
            error = "Đấu trường hiện hỗ trợ một Shadow Fox. Hãy chọn đội gồm Shadow Fox để tìm trận.";
            return false;
        }
        error = string.Empty;
        return true;
    }
}
