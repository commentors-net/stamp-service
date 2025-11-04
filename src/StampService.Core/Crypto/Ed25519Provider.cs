using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Security;
using StampService.Core.Interfaces;
using System.Text;

namespace StampService.Core.Crypto;

/// <summary>
/// Ed25519 cryptographic provider using BouncyCastle
/// </summary>
public class Ed25519Provider : ICryptoProvider
{
    public string Algorithm => "Ed25519";

    public (byte[] privateKey, byte[] publicKey) GenerateKeyPair()
    {
        var keyPairGenerator = new Ed25519KeyPairGenerator();
        keyPairGenerator.Init(new Ed25519KeyGenerationParameters(new SecureRandom()));
        
        var keyPair = keyPairGenerator.GenerateKeyPair();
        
        var privateKeyParams = (Ed25519PrivateKeyParameters)keyPair.Private;
        var publicKeyParams = (Ed25519PublicKeyParameters)keyPair.Public;
        
        return (privateKeyParams.GetEncoded(), publicKeyParams.GetEncoded());
    }

    public byte[] Sign(byte[] privateKey, byte[] data)
    {
        var privateKeyParams = new Ed25519PrivateKeyParameters(privateKey, 0);
        var signer = new Ed25519Signer();
        signer.Init(true, privateKeyParams);
        signer.BlockUpdate(data, 0, data.Length);
        return signer.GenerateSignature();
    }

    public bool Verify(byte[] publicKey, byte[] data, byte[] signature)
    {
        try
        {
            var publicKeyParams = new Ed25519PublicKeyParameters(publicKey, 0);
            var verifier = new Ed25519Signer();
            verifier.Init(false, publicKeyParams);
            verifier.BlockUpdate(data, 0, data.Length);
            return verifier.VerifySignature(signature);
        }
        catch
        {
            return false;
        }
    }

    public string ExportPublicKeyPem(byte[] publicKey)
    {
        var base64 = Convert.ToBase64String(publicKey);
        var sb = new StringBuilder();
        sb.AppendLine("-----BEGIN PUBLIC KEY-----");
        
        // Split into 64-character lines
        for (int i = 0; i < base64.Length; i += 64)
        {
            int length = Math.Min(64, base64.Length - i);
            sb.AppendLine(base64.Substring(i, length));
        }
        
        sb.AppendLine("-----END PUBLIC KEY-----");
        return sb.ToString();
    }

    public byte[] ImportPublicKeyPem(string pem)
    {
        var lines = pem.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(l => !l.StartsWith("-----"))
            .ToArray();
        
        var base64 = string.Join("", lines);
        return Convert.FromBase64String(base64);
    }
}
