using System.Security.Cryptography;

namespace StampService.Core.Interfaces;

/// <summary>
/// Interface for cryptographic operations
/// </summary>
public interface ICryptoProvider
{
    /// <summary>
    /// Algorithm name (e.g., "Ed25519", "ECDSA")
    /// </summary>
    string Algorithm { get; }

    /// <summary>
    /// Generate a new key pair
    /// </summary>
    (byte[] privateKey, byte[] publicKey) GenerateKeyPair();

    /// <summary>
    /// Sign data with the private key
    /// </summary>
    byte[] Sign(byte[] privateKey, byte[] data);

    /// <summary>
    /// Verify a signature
    /// </summary>
    bool Verify(byte[] publicKey, byte[] data, byte[] signature);

    /// <summary>
    /// Export public key to PEM format
    /// </summary>
    string ExportPublicKeyPem(byte[] publicKey);

    /// <summary>
    /// Import public key from PEM format
    /// </summary>
    byte[] ImportPublicKeyPem(string pem);
}
