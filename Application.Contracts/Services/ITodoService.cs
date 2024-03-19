﻿using Application.Contracts.Model;
using LanguageExt.Common;
using Package.Infrastructure.Data.Contracts;

namespace Application.Contracts.Services;

public interface ITodoService
{
    Task<PagedResponse<TodoItemDto>> GetPageAsync(int pageSize = 10, int pageIndex = 0, CancellationToken cancellationToken = default);
    Task<TodoItemDto?> GetItemAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<TodoItemDto?>> AddItemAsync(TodoItemDto dto, CancellationToken cancellationToken = default);
    Task<Result<TodoItemDto?>> UpdateItemAsync(TodoItemDto dto, CancellationToken cancellationToken = default);
    Task DeleteItemAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResponse<TodoItemDto>> GetPageExternalAsync(int pageSize = 10, int pageIndex = 0, CancellationToken cancellationToken = default);
}
