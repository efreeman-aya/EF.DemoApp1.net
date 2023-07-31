﻿using Microsoft.Extensions.DependencyInjection;
using Package.Infrastructure.Storage;
using Package.Infrastructure.Test.Integration.Blob;
using System.Text;

namespace Package.Infrastructure.Test.Integration;

[Ignore("Storage account required - Azurite storage emulator or a real Azure storage account.")]

[TestClass]
public class AzureBlobStorageTests : IntegrationTestBase
{
    private readonly IBlobRepository1 _blobRepo;

    public AzureBlobStorageTests()
    {
        _blobRepo = Services.GetRequiredService<IBlobRepository1>();
    }

    [TestMethod]
    public async Task UploadAndDownload_pass()
    {
        string data = "123,Spiderman,123546789,435762985762,2000.19\n543,Batman,987654321,23457692854,199.45\n";
        using MemoryStream uploadStream = new(Encoding.UTF8.GetBytes(data));

        //azure blob container name: length:3-63; allowed:lowercase,number,-
        string containerName = $"testcontainer-{Guid.NewGuid().ToString().ToLower()}";
        string blobName = $"testblob-{Guid.NewGuid().ToString().ToLower()}";

        CancellationToken token = new CancellationTokenSource().Token;

        ContainerInfo containerInfo = new()
        {
            ContainerName = containerName,
            ContainerPublicAccessType = ContainerPublicAccessType.None,
            CreateContainerIfNotExist = true
        };

        //upload 
        await _blobRepo.UploadBlobStreamAsync(containerInfo, blobName, uploadStream, cancellationToken: token);

        //reset
        string? dataDown;

        //download
        using (Stream downloadStream = await _blobRepo.DownloadBlobStreamAsync(containerInfo, blobName, cancellationToken: token))
        {
            StreamReader reader = new(downloadStream);
            dataDown = await reader.ReadToEndAsync();
        }

        await _blobRepo.DeleteBlobAsync(containerInfo, blobName, token);
        await _blobRepo.DeleteContainerAsync(containerInfo, token);

        Assert.IsTrue(data == dataDown);

    }
}
