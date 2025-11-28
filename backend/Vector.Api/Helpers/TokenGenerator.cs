namespace Vector.Api.Helpers;

public static class TokenGenerator
{
    public static string GenerateRandomToken(int length = 32)
    {
        var randomBytes = new byte[length];
        using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }
        return Convert.ToBase64String(randomBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
    }

    public static string GenerateEmailVerificationToken()
    {
        return GenerateRandomToken(32);
    }

    public static string GeneratePasswordResetToken()
    {
        return GenerateRandomToken(32);
    }
}

