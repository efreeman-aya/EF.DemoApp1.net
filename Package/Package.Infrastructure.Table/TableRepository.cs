﻿using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using System.Net;

namespace Package.Infrastructure.Table;

/// <summary>
/// https://learn.microsoft.com/en-us/dotnet/api/overview/azure/data.tables-readme?view=azure-dotnet
/// https://learn.microsoft.com/en-us/azure/storage/tables/table-storage-overview
/// https://github.com/Azure/azure-sdk-for-net/blob/Azure.Data.Tables_12.8.0/sdk/tables/Azure.Data.Tables/samples/README.md
/// </summary>
public abstract class TableRepository : ITableRepository
{
    private readonly IAzureClientFactory<TableServiceClient> _clientFactory;

    public TableRepository(ILogger<TableRepository> logger, IAzureClientFactory<TableServiceClient> clientFactory)
    {
        _clientFactory = clientFactory;
    }

    public async Task<T?> GetItemAsync<T>(string tableServiceClientName, string partitionKey, string rowkey, IEnumerable<string>? selectProps = null, CancellationToken cancellationToken = default)
        where T : class, ITableEntity
    {
        var client = _clientFactory.CreateClient(tableServiceClientName);
        var table = client.GetTableClient(typeof(T).Name);
        
        try
        {
            var response = await table.GetEntityAsync<T>(partitionKey, rowkey, selectProps, cancellationToken); //throws if no value (not found)
            return response.Value; 
        }
        catch (RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<HttpStatusCode> CreateItemAsync<T>(string tableServiceClientName, T item, CancellationToken cancellationToken = default)
        where T : ITableEntity
    {
        var client = _clientFactory.CreateClient(tableServiceClientName);
        var table = client.GetTableClient(typeof(T).Name);
        var response = await table.AddEntityAsync(item, cancellationToken);
        return (HttpStatusCode)response.Status;
    }

    public async Task<HttpStatusCode> UpsertItemAsync<T>(string tableServiceClientName, T item, TableUpdateMode updateMode, CancellationToken cancellationToken = default)
        where T : ITableEntity
    {
        var client = _clientFactory.CreateClient(tableServiceClientName);
        var table = client.GetTableClient(typeof(T).Name);
        var response = await table.UpsertEntityAsync(item, (Azure.Data.Tables.TableUpdateMode)updateMode, cancellationToken);
        return (HttpStatusCode)response.Status;
    }

    public async Task<HttpStatusCode> UpdateItemAsync<T>(string tableServiceClientName, T item, TableUpdateMode updateMode, CancellationToken cancellationToken = default)
        where T : ITableEntity
    {
        var client = _clientFactory.CreateClient(tableServiceClientName);
        var table = client.GetTableClient(typeof(T).Name);
        var response = await table.UpdateEntityAsync(item, item.ETag, (Azure.Data.Tables.TableUpdateMode)updateMode, cancellationToken);
        return (HttpStatusCode)response.Status;
    }

    public async Task<HttpStatusCode> DeleteItemAsync<T>(string tableServiceClientName, string partitionKey, string rowkey, CancellationToken cancellationToken = default)
    {
        var client = _clientFactory.CreateClient(tableServiceClientName);
        var table = client.GetTableClient(typeof(T).Name);
        var response = await table.DeleteEntityAsync(partitionKey, rowkey, cancellationToken: cancellationToken);
        return (HttpStatusCode)response.Status;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="tableServiceClientName"></param>
    /// <param name="continuationToken"></param>
    /// <param name="pageSize"></param>
    /// <param name="filterLinq"></param>
    /// <param name="selectProps"></param>
    /// <param name="includeTotal">Use with caution; requires retrieving all records - ugly, expensive.</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<(IReadOnlyList<T>?, int, string?)> QueryAsync<T>(string tableServiceClientName, string? continuationToken = null, 
        int pageSize = 10, Expression<Func<T, bool>>? filterLinq = null, string? filterOData = null, IEnumerable<string>? selectProps = null, bool includeTotal = false, 
        CancellationToken cancellationToken = default)
        where T : class, ITableEntity
    {
        var client = _clientFactory.CreateClient(tableServiceClientName);
        var table = client.GetTableClient(typeof(T).Name);

        var total = -1;

        //Tables do not offer an efficient count method,
        //so all records must be retrieved in order to get the count - ugly, expensive, use only when critical
        //minimize payload by projecting only a single column
        if (includeTotal)
        {
            total = 0;
            var pageableAll = filterLinq != null
                ? table.QueryAsync(filterLinq, null, new List<string>{ "PartitionKey" }, cancellationToken)
                : table.QueryAsync<T>(filterOData, null, new List<string> { "PartitionKey" }, cancellationToken);
            await foreach (var pageAll in pageableAll)
            {
                total++;
            }
        }

        //execute the query against the Table;
        //IAsyncEnumerable will retrieve all pages; only retrieve and return the first page here
        //let the client manage the paging with continuation token passed in for the next page
        var pageable = filterLinq != null
            ? table.QueryAsync(filterLinq, pageSize, selectProps, cancellationToken)
            : table.QueryAsync<T>(filterOData, pageSize, selectProps, cancellationToken);
        var enumerator = pageable.AsPages(continuationToken).GetAsyncEnumerator(cancellationToken);
        await enumerator.MoveNextAsync();
        var page = enumerator.Current;
        return (page.Values, total, page.ContinuationToken); 

    }

    public async Task<TableClient> GetOrCreateTableAsync(string tableServiceClientName, string tableName, CancellationToken cancellationToken)
    {
        var client = _clientFactory.CreateClient(tableServiceClientName);
        await client.CreateTableIfNotExistsAsync(tableName, cancellationToken);
        return client.GetTableClient(tableName);
    }

    public async Task<HttpStatusCode> DeleteTableAsync(string tableServiceClientName, string tableName, CancellationToken cancellationToken = default)
    {
        TableServiceClient client = _clientFactory.CreateClient(tableServiceClientName);
        var response = await client.DeleteTableAsync(tableName, cancellationToken);
        return (HttpStatusCode)response.Status;
    }
}
