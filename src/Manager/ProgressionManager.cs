using System;

public static class ProgressionManager
{
    public const int BaseLevelExperience = 100;
    public const int ExperienceGrowthPerLevel = 25;

    public static int GetRequiredExperienceForLevel(int level)
    {
        if (level < 1)
            level = 1;

        return BaseLevelExperience + (level - 1) * ExperienceGrowthPerLevel;
    }

    public static int GetTotalExperienceForLevel(int level)
    {
        if (level <= 1)
            return 0;

        int total = 0;
        for (int current = 1; current < level; current++)
        {
            total += GetRequiredExperienceForLevel(current);
        }

        return total;
    }

    public static void Normalize(AccountManager.AccountData account)
    {
        if (account == null)
            return;

        if (account.Level < 1)
            account.Level = 1;

        if (account.Experience < 0)
            account.Experience = 0;

        while (account.Experience >= GetRequiredExperienceForLevel(account.Level))
        {
            account.Experience -= GetRequiredExperienceForLevel(account.Level);
            account.Level++;
        }
    }
}
