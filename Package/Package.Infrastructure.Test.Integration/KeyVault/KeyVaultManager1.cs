﻿using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Package.Infrastructure.KeyVault;

namespace Package.Infrastructure.Test.Integration.KeyVault;
public class KeyVaultManager1 : KeyVaultManagerBase, IKeyVaultManager1
{
    public KeyVaultManager1(ILogger<KeyVaultManager1> logger, IOptions<KeyVaultManagerSettings1> settings,
        IAzureClientFactory<SecretClient> clientFactorySecret, IAzureClientFactory<KeyClient> clientFactoryKey, IAzureClientFactory<CertificateClient> clientFactoryCert)
        : base(logger, settings, clientFactorySecret, clientFactoryKey, clientFactoryCert)
    {

    }
}
