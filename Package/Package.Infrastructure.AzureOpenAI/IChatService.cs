﻿using OpenAI.Chat;

namespace Package.Infrastructure.AzureOpenAI;

public interface IChatService
{
    Task<(Guid, string)> ChatCompletionAsync(Guid? chatId, List<ChatMessage> newMessages, ChatCompletionOptions? options = null,
        Func<List<ChatMessage>, IReadOnlyList<ChatToolCall>, Task>? toolCallFunc = null, CancellationToken cancellationToken = default);
}
