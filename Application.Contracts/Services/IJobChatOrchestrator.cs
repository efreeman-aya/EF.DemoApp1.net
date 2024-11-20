﻿using Application.Contracts.Model;
using LanguageExt.Common;

namespace Application.Contracts.Services;

public interface IJobChatOrchestrator
{
    Task<Result<string>> ChatCompletionAsync(ChatRequest request, CancellationToken cancellationToken = default);
}
