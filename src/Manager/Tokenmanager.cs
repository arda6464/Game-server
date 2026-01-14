using System;
using System.Security.Cryptography;
using System.Text;

public static class TokenManager
{
    private const string Digits = "0123456789";
    private const string LettersAndDigits = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

  
    public static string GenerateNumericToken(int length = 12)
    {
        var result = new StringBuilder(length);
        byte[] buffer = new byte[length];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(buffer);
        }
        for (int i = 0; i < length; i++)
        {
            result.Append(Digits[buffer[i] % Digits.Length]);
        }
        return result.ToString();
    }

 
    public static string GeneratePlayerId(int length = 8)
    {
        var result = new StringBuilder(length);
        byte[] buffer = new byte[length];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(buffer);
        }
        for (int i = 0; i < length; i++)
        {
            result.Append(LettersAndDigits[buffer[i] % LettersAndDigits.Length]);
        }
        return result.ToString();
    }
}
