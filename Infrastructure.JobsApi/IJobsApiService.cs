﻿namespace Infrastructure.JobsApi;

public interface IJobsApiService
{
    Task<Lookups> GetLookupsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<int>> FindExpertiseMatchesAsync(string target, int maxCount, CancellationToken cancellationToken = default);
    Task<IEnumerable<Job>> SearchJobsAsync(List<int> expertiseCodes, decimal latitude, decimal longitude, int radiusMiles, int pageSize = 10);
}
