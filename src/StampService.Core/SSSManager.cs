using System.Security.Cryptography;
using System.Numerics;
using StampService.Core.Models;

namespace StampService.Core;

/// <summary>
/// Shamir Secret Sharing Manager
/// Implements threshold secret sharing for key backup and recovery
/// </summary>
public class SSSManager
{
    private readonly Random _random = new Random();
    
    /// <summary>
    /// Create shares from a secret using Shamir's Secret Sharing
    /// </summary>
    public ShareBundle CreateShares(byte[] secret, int totalShares, int threshold, string publicKey, string algorithm)
    {
        if (threshold > totalShares)
            throw new ArgumentException("Threshold cannot be greater than total shares");
        
        if (threshold < 2)
            throw new ArgumentException("Threshold must be at least 2");

        // Compute commitment (hash of secret)
        string commitment;
        using (var sha256 = SHA256.Create())
        {
            var hash = sha256.ComputeHash(secret);
            commitment = Convert.ToBase64String(hash);
        }

        var shares = new List<Share>();
        var secretBytes = secret;

        // For each byte of the secret, create polynomial shares
        var allShareData = new List<byte[]>();
        for (int i = 0; i < totalShares; i++)
        {
            allShareData.Add(new byte[secretBytes.Length]);
        }

        // Process each byte independently using Shamir's scheme
        for (int byteIndex = 0; byteIndex < secretBytes.Length; byteIndex++)
        {
            var byteShares = CreateByteShares(secretBytes[byteIndex], totalShares, threshold);
            for (int shareIndex = 0; shareIndex < totalShares; shareIndex++)
            {
                allShareData[shareIndex][byteIndex] = byteShares[shareIndex];
            }
        }

        // Create share objects
        for (int i = 0; i < totalShares; i++)
        {
            shares.Add(new Share
            {
                Index = i + 1,
                Data = Convert.ToBase64String(allShareData[i]),
                Threshold = threshold,
                TotalShares = totalShares,
                CreatedAt = DateTime.UtcNow,
                Commitment = commitment
            });
        }

        return new ShareBundle
        {
            Shares = shares,
            PublicKey = publicKey,
            Commitment = commitment,
            Algorithm = algorithm
        };
    }

    /// <summary>
    /// Verify that a share is valid against the commitment
    /// </summary>
    public ShareVerificationResult VerifyShare(Share share, string expectedCommitment)
    {
        // Basic validation
        if (share.Commitment != expectedCommitment)
        {
            return new ShareVerificationResult
            {
                IsValid = false,
                Message = "Share commitment does not match expected commitment",
                ShareIndex = share.Index
            };
        }

        // Verify share data is valid base64
        try
        {
            Convert.FromBase64String(share.Data);
        }
        catch
        {
            return new ShareVerificationResult
            {
                IsValid = false,
                Message = "Share data is not valid base64",
                ShareIndex = share.Index
            };
        }

        return new ShareVerificationResult
        {
            IsValid = true,
            Message = "Share is valid",
            ShareIndex = share.Index
        };
    }

    /// <summary>
    /// Reconstruct the secret from shares
    /// </summary>
    public byte[] ReconstructSecret(List<Share> shares)
    {
        if (shares.Count < shares[0].Threshold)
            throw new ArgumentException($"Need at least {shares[0].Threshold} shares to reconstruct secret");

        // Verify all shares have the same commitment
        var commitment = shares[0].Commitment;
        if (shares.Any(s => s.Commitment != commitment))
            throw new ArgumentException("Shares have different commitments");

        // Decode all share data
        var shareDataList = shares.Select(s => Convert.FromBase64String(s.Data)).ToList();
        var secretLength = shareDataList[0].Length;

        // Verify all shares have the same length
        if (shareDataList.Any(s => s.Length != secretLength))
            throw new ArgumentException("Shares have different lengths");

        var reconstructed = new byte[secretLength];

        // Reconstruct each byte using Lagrange interpolation
        for (int byteIndex = 0; byteIndex < secretLength; byteIndex++)
        {
            var byteShares = new List<(int x, byte y)>();
            for (int i = 0; i < shares.Count; i++)
            {
                byteShares.Add((shares[i].Index, shareDataList[i][byteIndex]));
            }

            reconstructed[byteIndex] = ReconstructByte(byteShares);
        }

        // Verify reconstruction
        string reconstructedCommitment;
        using (var sha256 = SHA256.Create())
        {
            var hash = sha256.ComputeHash(reconstructed);
            reconstructedCommitment = Convert.ToBase64String(hash);
        }

        if (reconstructedCommitment != commitment)
            throw new InvalidOperationException("Reconstructed secret does not match commitment");

        return reconstructed;
    }

    private byte[] CreateByteShares(byte secretByte, int totalShares, int threshold)
    {
        // Generate random coefficients for polynomial of degree (threshold - 1)
        var coefficients = new byte[threshold];
        coefficients[0] = secretByte; // The secret is the constant term

        using (var rng = RandomNumberGenerator.Create())
        {
            for (int i = 1; i < threshold; i++)
            {
                var randomByte = new byte[1];
                rng.GetBytes(randomByte);
                coefficients[i] = randomByte[0];
            }
        }

        // Evaluate polynomial at points 1, 2, ..., totalShares
        var shares = new byte[totalShares];
        for (int x = 1; x <= totalShares; x++)
        {
            shares[x - 1] = EvaluatePolynomial(coefficients, (byte)x);
        }

        return shares;
    }

    private byte EvaluatePolynomial(byte[] coefficients, byte x)
    {
        // Evaluate polynomial in GF(256)
        int result = 0;
        int xPower = 1;

        for (int i = 0; i < coefficients.Length; i++)
        {
            result ^= GF256Multiply(coefficients[i], (byte)xPower);
            xPower = GF256Multiply((byte)xPower, x);
        }

        return (byte)result;
    }

    private byte ReconstructByte(List<(int x, byte y)> shares)
    {
        // Lagrange interpolation in GF(256) to find f(0)
        int result = 0;

        foreach (var (xi, yi) in shares)
        {
            int numerator = 1;
            int denominator = 1;

            foreach (var (xj, _) in shares)
            {
                if (xi != xj)
                {
                    numerator = GF256Multiply((byte)numerator, (byte)xj);
                    denominator = GF256Multiply((byte)denominator, (byte)(xi ^ xj));
                }
            }

            var term = GF256Multiply(yi, GF256Multiply((byte)numerator, GF256Inverse((byte)denominator)));
            result ^= term;
        }

        return (byte)result;
    }

    // GF(256) arithmetic using AES polynomial
    private static readonly byte[] GF256_EXP = new byte[256];
    private static readonly byte[] GF256_LOG = new byte[256];

    static SSSManager()
    {
        InitializeGF256Tables();
    }

    private static void InitializeGF256Tables()
    {
        int poly = 0x11B; // AES polynomial: x^8 + x^4 + x^3 + x + 1
        int x = 1;

        for (int i = 0; i < 255; i++)
        {
            GF256_EXP[i] = (byte)x;
            GF256_LOG[x] = (byte)i;
            x <<= 1;
            if ((x & 0x100) != 0)
                x ^= poly;
        }
        GF256_EXP[255] = 1;
    }

    private static byte GF256Multiply(byte a, byte b)
    {
        if (a == 0 || b == 0)
            return 0;

        int logA = GF256_LOG[a];
        int logB = GF256_LOG[b];
        int sum = logA + logB;

        if (sum >= 255)
            sum -= 255;

        return GF256_EXP[sum];
    }

    private static byte GF256Inverse(byte a)
    {
        if (a == 0)
            throw new DivideByZeroException("Cannot invert zero in GF(256)");

        return GF256_EXP[255 - GF256_LOG[a]];
    }
}
