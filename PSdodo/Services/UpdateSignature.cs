using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PsExporter.Services;

/// <summary>Only the embedded release key can authenticate update metadata. Never trust a server-supplied key.</summary>
internal static class UpdateSignature
{
    private const string Algorithm = "RSA-SHA256-PKCS1";
    private const string Domain = "PSexp update manifest v1\n";
    private const int MaximumPayloadBytes = 48 * 1024;
    private static readonly Lazy<TrustedKey> ReleaseKey = new(LoadReleaseKey);

    public static UpdateManifest ReadVerified(ReadOnlyMemory<byte> document)
    {
        try
        {
            using var json = JsonDocument.Parse(document);
            var envelope = json.RootElement.GetProperty("signed_update");
            if (envelope.GetProperty("schema").GetInt32() != 1
                || envelope.GetProperty("algorithm").GetString() != Algorithm
                || envelope.GetProperty("key_id").GetString() != ReleaseKey.Value.KeyId)
                throw new IOException("更新签名格式或发布密钥不受信任");

            var payload = Convert.FromBase64String(envelope.GetProperty("payload").GetString() ?? "");
            var signature = Convert.FromBase64String(envelope.GetProperty("signature").GetString() ?? "");
            if (payload.Length is <= 0 or > MaximumPayloadBytes || signature.Length != 384)
                throw new IOException("更新签名或签名内容长度不正确");

            using var rsa = RSA.Create();
            rsa.FromXmlString(ReleaseKey.Value.PublicKeyXml);
            if (rsa.KeySize != 3072) throw new IOException("内置更新公钥长度不正确");
            var prefix = Encoding.UTF8.GetBytes(Domain);
            var authenticated = new byte[prefix.Length + payload.Length];
            prefix.CopyTo(authenticated, 0);
            payload.CopyTo(authenticated, prefix.Length);
            if (!rsa.VerifyData(authenticated, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1))
                throw new IOException("更新清单签名无效，已拒绝此更新");

            // The outer legacy fields are deliberately ignored. Every actionable field comes from signed bytes.
            var data = JsonSerializer.Deserialize<UpdateManifestData>(payload)
                ?? throw new IOException("已签名的更新清单为空");
            return new UpdateManifest(data);
        }
        catch (Exception error) when (error is JsonException or KeyNotFoundException or InvalidOperationException
                                      or FormatException or CryptographicException or ArgumentException)
        {
            throw new IOException("更新清单缺少有效的发布签名，已拒绝此更新", error);
        }
    }

    private static TrustedKey LoadReleaseKey()
    {
        using var stream = typeof(UpdateSignature).Assembly.GetManifestResourceStream(
            "PsExporter.Resources.Updates.update-public-key.json")
            ?? throw new IOException("找不到内置更新公钥");
        var key = JsonSerializer.Deserialize<TrustedKey>(stream)
            ?? throw new IOException("内置更新公钥为空");
        if (key.Algorithm != Algorithm || string.IsNullOrWhiteSpace(key.KeyId)
            || string.IsNullOrWhiteSpace(key.PublicKeyXml) || key.PublicKeyXml.Contains("<D>", StringComparison.Ordinal))
            throw new IOException("内置更新公钥格式不正确");
        return key;
    }

    private sealed class TrustedKey
    {
        [System.Text.Json.Serialization.JsonPropertyName("key_id")] public string KeyId { get; init; } = "";
        [System.Text.Json.Serialization.JsonPropertyName("algorithm")] public string Algorithm { get; init; } = "";
        [System.Text.Json.Serialization.JsonPropertyName("public_key_xml")] public string PublicKeyXml { get; init; } = "";
    }
}
