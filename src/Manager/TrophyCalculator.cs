using System;

public static class TrophyCalculator
{
    public static int GetFirstPlaceDelta(int playerCount)
    {
        if (playerCount <= 2) return 10;
        if (playerCount <= 6) return 12;
        if (playerCount <= 12) return 14;
        if (playerCount <= 20) return 16;
        return 18;
    }

    public static int GetLastPlaceDelta(int playerCount)
    {
        if (playerCount <= 2) return -8;
        if (playerCount <= 6) return -9;
        if (playerCount <= 12) return -10;
        if (playerCount <= 20) return -12;
        return -14;
    }

    public static int CalculateTrophyDelta(int playerCount, int placement)
    {
        if (playerCount <= 0)
            return 0;

        int first = GetFirstPlaceDelta(playerCount);
        int last = GetLastPlaceDelta(playerCount);

        if (placement <= 1)
            return first;

        if (placement >= playerCount)
            return last;

        float t = (placement - 1f) / (playerCount - 1f);
        return (int)MathF.Round(Lerp(first, last, t));
    }

    private static float Lerp(float a, float b, float t)
    {
        return a + (b - a) * t;
    }
}
