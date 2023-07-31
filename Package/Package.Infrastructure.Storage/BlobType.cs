﻿namespace Package.Infrastructure.Storage;

/// <summary>
/// Maps to Azure.Storage.Blobs.Models so client does not need that reference
/// </summary>
public enum BlobType
{
    Block,
    Page,
    Append
}
