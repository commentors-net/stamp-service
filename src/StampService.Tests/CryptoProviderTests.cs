using FluentAssertions;
using StampService.Core;
using StampService.Core.Crypto;
using System.Text;
using Xunit;

namespace StampService.Tests;

public class CryptoProviderTests
{
    [Fact]
    public void Ed25519Provider_Should_Generate_KeyPair()
    {
        // Arrange
        var provider = new Ed25519Provider();

        // Act
        var (privateKey, publicKey) = provider.GenerateKeyPair();

        // Assert
        privateKey.Should().NotBeNull();
        privateKey.Should().HaveCount(32); // Ed25519 private key is 32 bytes
        publicKey.Should().NotBeNull();
        publicKey.Should().HaveCount(32); // Ed25519 public key is 32 bytes
    }

    [Fact]
    public void Ed25519Provider_Should_Sign_And_Verify()
    {
        // Arrange
        var provider = new Ed25519Provider();
        var (privateKey, publicKey) = provider.GenerateKeyPair();
        var data = Encoding.UTF8.GetBytes("Test message to sign");

        // Act
        var signature = provider.Sign(privateKey, data);
        var isValid = provider.Verify(publicKey, data, signature);

        // Assert
        signature.Should().NotBeNull();
        signature.Should().HaveCount(64); // Ed25519 signature is 64 bytes
        isValid.Should().BeTrue();
    }

    [Fact]
    public void Ed25519Provider_Should_Reject_Invalid_Signature()
    {
        // Arrange
        var provider = new Ed25519Provider();
        var (privateKey, publicKey) = provider.GenerateKeyPair();
        var data = Encoding.UTF8.GetBytes("Test message");
        var signature = provider.Sign(privateKey, data);

        // Modify the signature
        signature[0] ^= 0xFF;

        // Act
        var isValid = provider.Verify(publicKey, data, signature);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void Ed25519Provider_Should_Export_And_Import_PublicKey_Pem()
    {
        // Arrange
        var provider = new Ed25519Provider();
        var (_, publicKey) = provider.GenerateKeyPair();

        // Act
        var pem = provider.ExportPublicKeyPem(publicKey);
        var importedKey = provider.ImportPublicKeyPem(pem);

        // Assert
        pem.Should().Contain("-----BEGIN PUBLIC KEY-----");
        pem.Should().Contain("-----END PUBLIC KEY-----");
        importedKey.Should().Equal(publicKey);
    }
}
