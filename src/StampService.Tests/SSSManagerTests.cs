using FluentAssertions;
using StampService.Core;
using System.Security.Cryptography;
using Xunit;

namespace StampService.Tests;

public class SSSManagerTests
{
    [Fact]
    public void SSSManager_Should_Create_Shares()
    {
        // Arrange
        var sssManager = new SSSManager();
        var secret = RandomNumberGenerator.GetBytes(32);
        var totalShares = 5;
        var threshold = 3;

        // Act
        var bundle = sssManager.CreateShares(secret, totalShares, threshold, "test-public-key", "Ed25519");

        // Assert
        bundle.Should().NotBeNull();
        bundle.Shares.Should().HaveCount(totalShares);
        bundle.Algorithm.Should().Be("Ed25519");
        bundle.Commitment.Should().NotBeNullOrEmpty();

        foreach (var share in bundle.Shares)
        {
            share.Threshold.Should().Be(threshold);
            share.TotalShares.Should().Be(totalShares);
            share.Data.Should().NotBeNullOrEmpty();
            share.Commitment.Should().Be(bundle.Commitment);
        }
    }

    [Fact]
    public void SSSManager_Should_Reconstruct_Secret_From_Threshold_Shares()
    {
        // Arrange
        var sssManager = new SSSManager();
        var secret = RandomNumberGenerator.GetBytes(32);
        var totalShares = 5;
        var threshold = 3;
        var bundle = sssManager.CreateShares(secret, totalShares, threshold, "test-public-key", "Ed25519");

        // Take only threshold shares (3 out of 5)
        var selectedShares = bundle.Shares.Take(threshold).ToList();

        // Act
        var reconstructed = sssManager.ReconstructSecret(selectedShares);

        // Assert
        reconstructed.Should().Equal(secret);
    }

    [Fact]
    public void SSSManager_Should_Reconstruct_Secret_From_Any_Threshold_Combination()
    {
        // Arrange
        var sssManager = new SSSManager();
        var secret = RandomNumberGenerator.GetBytes(32);
        var totalShares = 5;
        var threshold = 3;
        var bundle = sssManager.CreateShares(secret, totalShares, threshold, "test-public-key", "Ed25519");

        // Try different combinations of shares
        var combinations = new[]
        {
            new[] { 0, 1, 2 }, // First 3
            new[] { 2, 3, 4 }, // Last 3
            new[] { 0, 2, 4 }, // Non-sequential
            new[] { 1, 2, 3 }, // Middle 3
        };

        foreach (var combination in combinations)
        {
            // Act
            var selectedShares = combination.Select(i => bundle.Shares[i]).ToList();
            var reconstructed = sssManager.ReconstructSecret(selectedShares);

            // Assert
            reconstructed.Should().Equal(secret, $"combination {string.Join(",", combination)} should work");
        }
    }

    [Fact]
    public void SSSManager_Should_Fail_With_Insufficient_Shares()
    {
        // Arrange
        var sssManager = new SSSManager();
        var secret = RandomNumberGenerator.GetBytes(32);
        var totalShares = 5;
        var threshold = 3;
        var bundle = sssManager.CreateShares(secret, totalShares, threshold, "test-public-key", "Ed25519");

        // Take only 2 shares (less than threshold)
        var selectedShares = bundle.Shares.Take(2).ToList();

        // Act & Assert
        var action = () => sssManager.ReconstructSecret(selectedShares);
        action.Should().Throw<ArgumentException>()
            .WithMessage("*at least*");
    }

    [Fact]
    public void SSSManager_Should_Verify_Valid_Share()
    {
        // Arrange
        var sssManager = new SSSManager();
        var secret = RandomNumberGenerator.GetBytes(32);
        var bundle = sssManager.CreateShares(secret, 5, 3, "test-public-key", "Ed25519");
        var share = bundle.Shares[0];

        // Act
        var result = sssManager.VerifyShare(share, bundle.Commitment);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.ShareIndex.Should().Be(share.Index);
    }

    [Fact]
    public void SSSManager_Should_Reject_Share_With_Wrong_Commitment()
    {
        // Arrange
        var sssManager = new SSSManager();
        var secret = RandomNumberGenerator.GetBytes(32);
        var bundle = sssManager.CreateShares(secret, 5, 3, "test-public-key", "Ed25519");
        var share = bundle.Shares[0];
        var wrongCommitment = "wrong-commitment-hash";

        // Act
        var result = sssManager.VerifyShare(share, wrongCommitment);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Message.Should().Contain("commitment");
    }

    [Fact]
    public void SSSManager_Should_Detect_Mismatched_Commitments_During_Reconstruction()
    {
        // Arrange
        var sssManager = new SSSManager();
        var secret1 = RandomNumberGenerator.GetBytes(32);
        var secret2 = RandomNumberGenerator.GetBytes(32);
        
        var bundle1 = sssManager.CreateShares(secret1, 5, 3, "key1", "Ed25519");
        var bundle2 = sssManager.CreateShares(secret2, 5, 3, "key2", "Ed25519");

        // Mix shares from different bundles
        var mixedShares = new List<Core.Models.Share>
        {
            bundle1.Shares[0],
            bundle2.Shares[1], // From different bundle!
            bundle1.Shares[2]
        };

        // Act & Assert
        var action = () => sssManager.ReconstructSecret(mixedShares);
        action.Should().Throw<ArgumentException>()
            .WithMessage("*different commitments*");
    }
}
