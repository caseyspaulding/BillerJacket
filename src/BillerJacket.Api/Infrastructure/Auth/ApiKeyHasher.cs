using System.Security.Cryptography;
using System.Text;

namespace BillerJacket.Api.Infrastructure.Auth;

public static class ApiKeyHasher
{
    public static string Hash(string rawKey)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawKey));
        return Convert.ToBase64String(bytes);
    }
}
