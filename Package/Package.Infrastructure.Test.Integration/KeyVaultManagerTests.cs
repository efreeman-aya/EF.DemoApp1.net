﻿using Azure;
using Azure.Security.KeyVault.Keys;
using Microsoft.Extensions.DependencyInjection;
using Package.Infrastructure.Test.Integration.KeyVault;

namespace Package.Infrastructure.Test.Integration;

[Ignore("Key Vault setup required.")]

[TestClass]
public class KeyVaultManagerTests : IntegrationTestBase
{
    private readonly IKeyVaultManager1 _vault;

    public KeyVaultManagerTests()
    {
        _vault = Services.GetRequiredService<IKeyVaultManager1>();
    }

    [TestMethod]
    public async Task Secret_crud_pass()
    {
        var secretName = $"secret-{Guid.NewGuid()}";
        var secretValue = "some-secret-value";
        var secretValueUpdated = $"update {secretValue}";

        var response = await _vault.SaveSecretAsync(secretName, secretValue);
        Assert.AreEqual(secretValue, response);

        response = await _vault.GetSecretAsync(secretName);
        Assert.AreEqual(secretValue, response);

        response = await _vault.SaveSecretAsync(secretName, secretValueUpdated);
        Assert.AreEqual(secretValueUpdated, response);

        response = await _vault.GetSecretAsync(secretName);
        Assert.AreEqual(secretValueUpdated, response);

        response = (await _vault.StartDeleteSecretAsync(secretName)).Name;
        Assert.AreEqual(secretName, response);

        try
        {
            response = await _vault.GetSecretAsync(secretName);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "SecretNotFound")
        {
            Assert.IsTrue(true);
        }
    }

    [TestMethod]
    public async Task Key_crud_pass()
    {
        var keyName = $"key-{Guid.NewGuid()}";

        var jwk = await _vault.CreateKeyAsync(keyName, KeyType.Rsa);
        Assert.IsNotNull(jwk);
        jwk = await _vault.GetKeyAsync(keyName);
        Assert.IsNotNull(jwk);
        jwk = await _vault.RotateKeyAsync(keyName);
        Assert.IsNotNull(jwk);
        jwk = await _vault.DeleteKeyAsync(keyName);
        Assert.IsNotNull(jwk);
        try
        {
            jwk = await _vault.GetKeyAsync(keyName);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "KeyNotFound")
        {
            Assert.IsTrue(true);
        }
    }

    [TestMethod]
    public async Task Cert_get_pass()
    {
        //Cert must exist in the KeyVault
        var certName = $"existing-cert-name";

        //X.509 certificate - the public key and cert metadata.
        var certBytes = await _vault.GetCertAsync(certName);
        Assert.IsNotNull(certBytes);

        //X.509 certificate - the private key.
        var certKey = await _vault.GetKeyAsync(certName);
        Assert.IsNotNull(certKey);

        //X.509 certificate - export the full X.509 certificate, including its private key (if its policy allows for private key exporting).
        var certSecret = await _vault.GetSecretAsync(certName);
        Assert.IsNotNull(certSecret);

    }
}
