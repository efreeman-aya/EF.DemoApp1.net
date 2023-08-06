﻿using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Package.Infrastructure.KeyVault;

public abstract class KeyVaultManagerBase : IKeyVaultManager
{
    private readonly ILogger<KeyVaultManagerBase> _logger;
    private readonly KeyVaultManagerSettingsBase _settings;
    private readonly SecretClient _secretClient;
    private readonly KeyClient _keyClient;
    private readonly CertificateClient _certClient;
    
    protected KeyVaultManagerBase(ILogger<KeyVaultManagerBase> logger, IOptions<KeyVaultManagerSettingsBase> settings,
        IAzureClientFactory<SecretClient> clientFactorySecret, IAzureClientFactory<KeyClient> clientFactoryKey, IAzureClientFactory<CertificateClient> clientFactoryCert)
    {
        _logger = logger;
        _settings = settings.Value;
        _secretClient = clientFactorySecret.CreateClient(_settings.KeyVaultClientName);
        _keyClient = clientFactoryKey.CreateClient(_settings.KeyVaultClientName);
        _certClient = clientFactoryCert.CreateClient(_settings.KeyVaultClientName);
    }

    /// <summary>
    /// Get a specified secret from a given key vault.
    /// </summary>
    /// <remarks>
    /// The get operation is applicable to any secret stored in Azure Key Vault.
    /// This operation requires the secrets/get permission.
    /// </remarks>
    /// <param name="name">The name of the secret.</param>
    /// <param name="version">The version of the secret.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
    /// <exception cref="ArgumentException"><paramref name="name"/> is an empty string.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is null.</exception>
    /// <exception cref="RequestFailedException">The server returned an error. See <see cref="Exception.Message"/> for details returned from the server.</exception>
    public async Task<string?> GetSecretAsync(string name, string? version = null, CancellationToken cancellationToken = default)
    {
        _ = _settings.GetHashCode(); //compilet warning

        _logger.LogInformation("GetSecretAsync - {name} {version}", name, version);
        var response =  await _secretClient.GetSecretAsync(name, version, cancellationToken);
        return response.Value.Value;
    }

    /// <summary>
    /// Sets a secret in a specified key vault.
    /// </summary>
    /// <remarks>
    /// The set operation adds a secret to the Azure Key Vault. If the named secret
    /// already exists, Azure Key Vault creates a new version of that secret. This
    /// operation requires the secrets/set permission.
    /// </remarks>
    /// <param name="name">The name of the secret. It must not be null.</param>
    /// <param name="value">The value of the secret. It must not be null.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
    /// <exception cref="ArgumentException"><paramref name="name"/> is an empty string.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is null.</exception>
    /// <exception cref="RequestFailedException">The server returned an error. See <see cref="Exception.Message"/> for details returned from the server.</exception>
    public async Task<string?> SaveSecretAsync(string name, string? value = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("SaveSecretAsync - {name}", name);
        var response = await _secretClient.SetSecretAsync(name, value, cancellationToken);
        return response.Value.Value;
    }

    /// <summary>
    /// Deletes a secret from a specified key vault.
    /// </summary>
    /// <remarks>
    /// The delete operation applies to any secret stored in Azure Key Vault.
    /// Delete cannot be applied to an individual version of a secret. This
    /// operation requires the secrets/delete permission.
    /// </remarks>
    /// <param name="name">The name of the secret.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
    /// <returns>
    /// A <see cref="DeleteSecretOperation"/> to wait on this long-running operation.
    /// If the Key Vault is soft delete-enabled, you only need to wait for the operation to complete if you need to recover or purge the secret;
    /// otherwise, the secret is deleted automatically on the <see cref="DeletedSecret.ScheduledPurgeDate"/>.
    /// </returns>
    /// <exception cref="ArgumentException"><paramref name="name"/> is an empty string.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is null.</exception>
    /// <exception cref="RequestFailedException">The server returned an error. See <see cref="Exception.Message"/> for details returned from the server.</exception>
    public async Task<DeletedSecret> StartDeleteSecretAsync(string name, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("StartDeleteSecretAsync - {name}", name);
        var response = await _secretClient.StartDeleteSecretAsync(name, cancellationToken);
        return response.Value;
    }

    /// <summary>
    /// Gets the public part of a stored key.
    /// </summary>
    /// <remarks>
    /// The get key operation is applicable to all key types. If the requested key
    /// is symmetric, then no key is released in the response. This
    /// operation requires the keys/get permission.
    /// </remarks>
    /// <param name="name">The name of the key.</param>
    /// <param name="version">The version of the key.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
    /// <exception cref="ArgumentException"><paramref name="name"/> is an empty string.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is null.</exception>
    /// <exception cref="RequestFailedException">The server returned an error. See <see cref="Exception.Message"/> for details returned from the server.</exception>
    public async Task<JsonWebKey?> GetKeyAsync(string name, string? version = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GetKeyAsync - {name} {version}", name, version);
        var response = await _keyClient.GetKeyAsync(name, version, cancellationToken);
        return response.Value.Key;
    }

    /// <summary>
    /// Creates and stores a new key in Key Vault. The create key operation can be used to create any key type in Azure Key Vault.
    /// If the named key already exists, Azure Key Vault creates a new version of the key. This operation requires the keys/create permission.
    /// </summary>
    /// <param name="name">The name of the key.</param>
    /// <param name="keyType">The type of key to create. See <see cref="KeyType"/> for valid values.</param>
    /// <param name="keyOptions">Specific attributes with information about the key.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
    /// <exception cref="ArgumentException"><paramref name="name"/> is an empty string, or <paramref name="keyType"/> contains no value.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is null.</exception>
    /// <exception cref="RequestFailedException">The server returned an error. See <see cref="Exception.Message"/> for details returned from the server.</exception>
    public async Task<JsonWebKey?> CreateKeyAsync(string name, KeyType keyType, CreateKeyOptions? options = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("CreateKeyAsync - {name} {keyType}", name, keyType);
        var response = await _keyClient.CreateKeyAsync(name, keyType, options, cancellationToken);
        return response.Value.Key;
    }

    /// <summary>
    /// Updates the <see cref="KeyRotationPolicy"/> for the specified key in Key Vault.
    /// The new policy will be used for the next version of the key when rotated.
    /// </summary>
    /// <param name="keyName">The name of the key.</param>
    /// <param name="policy">The <see cref="KeyRotationPolicy"/> to update.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
    /// <remarks>
    /// This operation requires the keys/update permission.
    /// </remarks>
    /// <returns>A <see cref="KeyRotationPolicy"/> for the specified key.</returns>
    /// <exception cref="ArgumentException"><paramref name="keyName"/> contains an empty string.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="keyName"/> or <paramref name="policy"/> is null.</exception>
    /// <exception cref="RequestFailedException">The server returned an error. See <see cref="Exception.Message"/> for details returned from the server.</exception>
    public async Task<KeyRotationPolicy> UpdateKeyRotationPolicyAsync(string name, KeyRotationPolicy policy, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("UpdateKeyRotationPolicyAsync - {name}", name);
        var response = await _keyClient.UpdateKeyRotationPolicyAsync(name, policy, cancellationToken);
        return response.Value;
    }

    /// <summary>
    /// Creates a new key version in Key Vault, stores it, then returns the new <see cref="KeyVaultKey"/>.
    /// </summary>
    /// <param name="name">The name of key to be rotated. The system will generate a new version in the specified key.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
    /// <remarks>
    /// The operation will rotate the key based on the key policy. It requires the keys/rotate permission.
    /// </remarks>
    /// <returns>A new version of the rotate <see cref="KeyVaultKey"/>.</returns>
    /// <exception cref="ArgumentException"><paramref name="name"/> contains an empty string.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is null.</exception>
    /// <exception cref="RequestFailedException">The server returned an error. See <see cref="Exception.Message"/> for details returned from the server.</exception>
    public async Task<JsonWebKey?> RotateKeyAsync(string name, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("RotateKeyAsync - {name}", name);
        var response = await _keyClient.RotateKeyAsync(name, cancellationToken);
        return response.Value.Key;
    }

    /// <summary>
    /// Deletes a key of any type from storage in Azure Key Vault.
    /// </summary>
    /// <remarks>
    /// The delete key operation cannot be used to remove individual versions of a
    /// key. This operation removes the cryptographic material associated with the
    /// key, which means the key is not usable for Sign/Verify, Wrap/Unwrap or
    /// Encrypt/Decrypt operations. This operation requires the keys/delete
    /// permission.
    /// </remarks>
    /// <param name="name">The name of the key.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
    /// <returns>
    /// A <see cref="DeleteKeyOperation"/> to wait on this long-running operation.
    /// If the Key Vault is soft delete-enabled, you only need to wait for the operation to complete if you need to recover or purge the key;
    /// otherwise, the key is deleted automatically on the <see cref="DeletedKey.ScheduledPurgeDate"/>.
    /// </returns>
    /// <exception cref="ArgumentException"><paramref name="name"/> is an empty string.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is null.</exception>
    /// <exception cref="RequestFailedException">The server returned an error. See <see cref="Exception.Message"/> for details returned from the server.</exception>
    public async Task<JsonWebKey?> DeleteKeyAsync(string name, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("DeleteKeyAsync - {name}", name);
        var response = await _keyClient.StartDeleteKeyAsync(name, cancellationToken);
        return response.Value.Key;
    }

    /// <summary>
    /// Returns the latest version of the <see cref="KeyVaultCertificate"/> along with its <see cref="CertificatePolicy"/>. This operation requires the certificates/get permission.
    /// </summary>
    /// <param name="certificateName">The name of the <see cref="KeyVaultCertificate"/> to retrieve.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
    /// <returns>A response containing the certificate and policy as a <see cref="KeyVaultCertificateWithPolicy"/> instance.</returns>
    /// <exception cref="ArgumentException"><paramref name="certificateName"/> is empty.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="certificateName"/> is null.</exception>
    public async Task<byte[]?> GetCertAsync(string certificateName, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GetCertAsync - {name}", certificateName);
        var response = await _certClient.GetCertificateAsync(certificateName, cancellationToken);
        return response.Value.Cer;
    }

    /// <summary>
    /// Imports a pre-existing certificate to the key vault. The specified certificate must be in PFX or ASCII PEM-format, and must contain the private key as well as the X.509 certificates. This operation requires the
    /// certificates/import permission.
    /// </summary>
    /// <param name="importCertificateOptions">The details of the certificate to import to the key vault.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
    /// <returns>The imported certificate and policy.</returns>
    /// <exception cref="ArgumentException"><see cref="ImportCertificateOptions.Name"/> of <paramref name="importCertificateOptions"/> is empty.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="importCertificateOptions"/> or <see cref="ImportCertificateOptions.Name"/> of <paramref name="importCertificateOptions"/> is null.</exception>
    public async Task<byte[]?> ImportCertAsync(ImportCertificateOptions importCertificateOptions, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("ImportCertAsync");
        var response = await _certClient.ImportCertificateAsync(importCertificateOptions, cancellationToken);
        return response.Value.Cer;
    }

    /// <summary>
    /// Starts a long running operation to create a <see cref="KeyVaultCertificate"/> in the vault with the specified certificate policy.
    /// </summary>
    /// <remarks>
    /// If no certificate with the specified name exists it will be created; otherwise, a new version of the existing certificate will be created.
    /// This operation requires the certificates/create permission.
    /// </remarks>
    /// <param name="certificateName">The name of the certificate to create.</param>
    /// <param name="policy">The <see cref="CertificatePolicy"/> which governs the properties and lifecycle of the created certificate.</param>
    /// <param name="enabled">Specifies whether the certificate should be created in an enabled state. If null, the server default will be used.</param>
    /// <param name="tags">Tags to be applied to the created certificate.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
    /// <returns>A <see cref="CertificateOperation"/> which contains details on the create operation, and can be used to retrieve updated status.</returns>
    /// <exception cref="ArgumentException"><paramref name="certificateName"/> is empty.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="certificateName"/> or <paramref name="policy"/> is null.</exception>
    public async Task<CertificateOperation> StartCreateCertAsync(string certificateName, CertificatePolicy policy, bool? enabled = default, IDictionary<string, string> tags = default, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("StartCreateCertificateAsync - {name}", certificateName);
        var response = await _certClient.StartCreateCertificateAsync(certificateName, policy, enabled, tags, cancellationToken);
        return response;
    }

    /// <summary>
    /// Deletes all versions of the specified <see cref="KeyVaultCertificate"/>. If the vault is soft delete-enabled, the <see cref="KeyVaultCertificate"/> will be marked for permanent deletion
    /// and can be recovered with <see cref="StartRecoverDeletedCertificate"/>, or purged with <see cref="PurgeDeletedCertificate"/>. This operation requires the certificates/delete permission.
    /// </summary>
    /// <param name="certificateName">The name of the <see cref="KeyVaultCertificate"/> to delete.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
    /// <returns>
    /// A <see cref="Certificates.DeleteCertificateOperation"/> to wait on this long-running operation.
    /// If the Key Vault is soft delete-enabled, you only need to wait for the operation to complete if you need to recover or purge the certificate;
    /// otherwise, the certificate is deleted automatically on the <see cref="DeletedCertificate.ScheduledPurgeDate"/>.
    /// </returns>
    /// <exception cref="ArgumentException"><paramref name="certificateName"/> is empty.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="certificateName"/> is null.</exception>
    public async Task<DeleteCertificateOperation?> StartDeleteCertificateAsync(string certificateName, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("StartDeleteCertificateAsync - {certificateName}", certificateName);
        var response = await _certClient.StartDeleteCertificateAsync(certificateName, cancellationToken);
        return response;
    }
}
