using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SpawnDev.BlazorJS.WebWorkers.Build.Tasks;

public static class FileHasher
{
    public static string GetStringHashBase64(string text)
    {
        using var hash = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(text);
        var hashBytes = hash.ComputeHash(bytes);
        return Convert.ToBase64String(hashBytes);
    }
    public static string GetFileHashBase64(string filePath)
    {
        var bytes = File.ReadAllBytes(filePath);
        using var hash = SHA256.Create();
        var hashBytes = hash.ComputeHash(bytes);
        return Convert.ToBase64String(hashBytes);
    }
}
