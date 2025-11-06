using FluentAssertions;
using StampService.Core;
using StampService.Core.Crypto;
using System.Text;
using Xunit;

namespace StampService.Tests;

public class KeyManagerTests : IDisposable
{
    private readonly string _testKeyPath;
    private readonly string _testLogPath;
    private readonly KeyManager _keyManager;
    private readonly AuditLogger _auditLogger;

    public KeyManagerTests()
    {
        var testDir = Path.Combine(Path.GetTempPath(), "StampServiceTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(testDir);
        
        _testKeyPath = Path.Combine(testDir, "test.key");
        _testLogPath = Path.Combine(testDir, "test.log");
        
        _auditLogger = new AuditLogger(_testLogPath);
        var cryptoProvider = new Ed25519Provider();
        _keyManager = new KeyManager(cryptoProvider, _auditLogger, _testKeyPath);
    }

    [Fact]
    public void KeyManager_Should_Generate_Key()
    {
        // Act
        _keyManager.GenerateKey();

        // Assert
        _keyManager.HasKey.Should().BeTrue();
        _keyManager.Algorithm.Should().Be("Ed25519");
        File.Exists(_testKeyPath).Should().BeTrue();
    }

    [Fact]
    public void KeyManager_Should_Load_Saved_Key()
    {
        // Arrange
        _keyManager.GenerateKey();
        var publicKeyPem = _keyManager.GetPublicKeyPem();

        // Create a new KeyManager instance
        var cryptoProvider = new Ed25519Provider();
        using var newKeyManager = new KeyManager(cryptoProvider, _auditLogger, _testKeyPath);

        // Act
        var loaded = newKeyManager.LoadKey();

        // Assert
        loaded.Should().BeTrue();
        newKeyManager.HasKey.Should().BeTrue();
        newKeyManager.GetPublicKeyPem().Should().Be(publicKeyPem);
    }

    [Fact]
    public void KeyManager_Should_Sign_Data()
    {
        // Arrange
        _keyManager.GenerateKey();
        var data = Encoding.UTF8.GetBytes("Test message");

        // Act
        var signature = _keyManager.Sign(data);

        // Assert
        signature.Should().NotBeNull();
        signature.Should().HaveCount(64); // Ed25519 signature length
    }

    [Fact]
    public void KeyManager_Should_Verify_Signature()
    {
        // Arrange
        _keyManager.GenerateKey();
        var data = Encoding.UTF8.GetBytes("Test message");
        var signature = _keyManager.Sign(data);

        // Act
        var isValid = _keyManager.Verify(data, signature);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void KeyManager_Should_Compute_Commitment()
    {
        // Arrange
        _keyManager.GenerateKey();

        // Act
        var commitment = _keyManager.ComputeCommitment();

        // Assert
        commitment.Should().NotBeNullOrEmpty();
        commitment.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void KeyManager_Should_Return_False_When_Loading_Nonexistent_Key()
    {
        // Act
        var loaded = _keyManager.LoadKey();

        // Assert
        loaded.Should().BeFalse();
        _keyManager.HasKey.Should().BeFalse();
    }

    [Fact]
    public void KeyManager_Should_Throw_When_Signing_Without_Key()
    {
        // Arrange
        var data = Encoding.UTF8.GetBytes("Test message");

        // Act & Assert
        var action = () => _keyManager.Sign(data);
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*No private key*");
    }

    [Fact]
    public void KeyManager_Should_Export_Public_Key_Pem()
    {
        // Arrange
        _keyManager.GenerateKey();

        // Act
        var pem = _keyManager.GetPublicKeyPem();

        // Assert
        pem.Should().Contain("-----BEGIN PUBLIC KEY-----");
        pem.Should().Contain("-----END PUBLIC KEY-----");
    }

    [Fact]
    public void KeyManager_Should_Delete_Key_When_Confirmed()
    {
        // Arrange
        _keyManager.GenerateKey();
        _keyManager.HasKey.Should().BeTrue();
        File.Exists(_testKeyPath).Should().BeTrue();

        // Act
        _keyManager.DeleteKey(confirmDeletion: true);

        // Assert
        _keyManager.HasKey.Should().BeFalse();
        File.Exists(_testKeyPath).Should().BeFalse();
    }

    [Fact]
    public void KeyManager_Should_Throw_When_Deleting_Without_Confirmation()
    {
        // Arrange
        _keyManager.GenerateKey();

     // Act & Assert
        var action = () => _keyManager.DeleteKey(confirmDeletion: false);
        action.Should().Throw<InvalidOperationException>()
  .WithMessage("*must be explicitly confirmed*");
    }

    [Fact]
    public void KeyManager_Should_Clear_Keys_From_Memory_After_Deletion()
    {
        // Arrange
        _keyManager.GenerateKey();
    var publicKeyBefore = _keyManager.GetPublicKeyPem();
        publicKeyBefore.Should().NotBeNullOrEmpty();

        // Act
 _keyManager.DeleteKey(confirmDeletion: true);

        // Assert
        _keyManager.HasKey.Should().BeFalse();
        
        // Attempting to sign should fail
        var data = Encoding.UTF8.GetBytes("Test");
        var action = () => _keyManager.Sign(data);
 action.Should().Throw<InvalidOperationException>()
            .WithMessage("*No private key*");
    }

    [Fact]
    public void KeyManager_Should_Handle_Delete_When_No_Key_Exists()
    {
        // Arrange - no key generated
        _keyManager.HasKey.Should().BeFalse();

// Act - should not throw
        _keyManager.DeleteKey(confirmDeletion: true);

        // Assert
    _keyManager.HasKey.Should().BeFalse();
        File.Exists(_testKeyPath).Should().BeFalse();
    }

    [Fact]
    public void KeyManager_Should_Not_Load_After_Deletion()
    {
     // Arrange
        _keyManager.GenerateKey();
      var publicKeyBefore = _keyManager.GetPublicKeyPem();
        
        // Act
        _keyManager.DeleteKey(confirmDeletion: true);

        // Try to load
var loaded = _keyManager.LoadKey();

        // Assert
        loaded.Should().BeFalse();
        _keyManager.HasKey.Should().BeFalse();
    }

    [Fact]
    public void KeyManager_Should_Allow_New_Key_Generation_After_Deletion()
    {
        // Arrange
    _keyManager.GenerateKey();
        var firstPublicKey = _keyManager.GetPublicKeyPem();
        
        // Act - Delete
        _keyManager.DeleteKey(confirmDeletion: true);
 _keyManager.HasKey.Should().BeFalse();

        // Act - Generate new key
     _keyManager.GenerateKey();

        // Assert
    _keyManager.HasKey.Should().BeTrue();
 var newPublicKey = _keyManager.GetPublicKeyPem();
        newPublicKey.Should().NotBe(firstPublicKey); // Should be different
    }

 [Fact]
    public void KeyManager_Should_Check_Key_File_Exists()
    {
        // Arrange - no key
        _keyManager.KeyFileExists().Should().BeFalse();

        // Act - generate key
  _keyManager.GenerateKey();

        // Assert
        _keyManager.KeyFileExists().Should().BeTrue();

      // Act - delete key
        _keyManager.DeleteKey(confirmDeletion: true);

// Assert
        _keyManager.KeyFileExists().Should().BeFalse();
    }

    public void Dispose()
    {
        _keyManager?.Dispose();
        
        // Clean up test files
        var testDir = Path.GetDirectoryName(_testKeyPath);
        if (!string.IsNullOrEmpty(testDir) && Directory.Exists(testDir))
        {
            try
            {
                Directory.Delete(testDir, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}
