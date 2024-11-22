﻿using Infrastructure.JobsApi;
using LanguageExt.Common;
using OpenAI.Chat;
using System.Text.Json;

namespace Application.Services.JobChat;

public class JobChatOrchestrator(ILogger<JobChatOrchestrator> logger, IOptions<JobChatOrchestratorSettings> settings, IJobChatService chatService, IJobsApiService jobsService)
    : ServiceBase(logger), IJobChatOrchestrator
{

    public async Task<Result<ChatResponse>> ChatCompletionAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        var messages = new List<ChatMessage>();
        if (request.ChatId == null)
        {
            messages.Add(new SystemChatMessage(InitialSystemMessage()));
        }

        messages.Add(new UserChatMessage(request.Message));

        //options for the chat - identify the tools available to the model
        ChatCompletionOptions options = new()
        {
            Tools = { findExpertiseMatches, searchJobs }
        };

        try
        {
            var response = await chatService.ChatCompletionAsync(request.ChatId, messages, options, ToolsCallback, settings.Value.MaxMessageCount, cancellationToken);
            return new ChatResponse(response.Item1, response.Item2);
        }
        catch (Exception ex)
        {
            return new Result<ChatResponse>(ex);
        }

    }

    private static string InitialSystemMessage()
    {
        //Once the location, distance, and expertises are defined, you will give a concise summarization, and ask the user to confirm or change any details.
        //based on only valid expertise names, latitude, longitude, and radius.
        //You will validate the user input against a valid list of expertise names before searching jobs.
        //, considering the user input to identify matching valid expertises

        var systemPrompt = @"
###
You are a professional assistant that helps people find the job they are looking for, introduce yourself and your mission.
The user must enter search criteria consisting of a list of expertises and an optional location and distance, or be willing to travel anywhere. 
If you are unable to find a matching allowed expertise, let the person know there is no match, and tell them a joke about the missing expertise.
###
After the allowed expertise list has been collected, and optional location and distance, present a summary of search criteria
in a html unordered confirmation list, and ask the user to confirm.
###
If a location is provided, you calculate the latitude and longitude for the job search
You can only perform a search with at least one allowed expertise or several (and location, if provided). If there are no allowed expertise's, 
you will reply that you are unable to search and make a joke about it.
###
Always present the user with an html search results table, containing only the jobs found in the search results.
Include up to 10 jobs and relevant details such as required certifications 
and shift hours if applicable, compensation range, with link 'More details and Apply' link to the specific job application on the job website
using the format https://www.ayahealthcare.com/travel-nursing-job/{JobId} to open in a new tab.
###
Sample confirmation list:
<ul>
    <li><strong>Expertise:</strong> ER</li>
    <li><strong>Location:</strong> San Diego</li>
    <li><strong>Distance:</strong> 20 miles</li>
</ul>
###
Sample search results table:
<table>
    <tbody><tr>
        <th>Facility Name</th>
        <th>Position</th>
        <th>Employment Type</th>
        <th>Shift Hours</th>
        <th>Compensation Range</th>
        <th>More details and Apply</th>
    </tr>
    <tr>
        <td>Sharp Memorial Hospital</td>
        <td>Registered Nurse</td>
        <td>Travel/Contract</td>
        <td>19:00 - 07:00 (3x12-Hour)</td>
        <td>$2,464 - $2,693</td>
        <td><a href=""https://www.ayahealthcare.com/travel-nursing-job/2676054"" target=""_blank"">More details and Apply</a></td>
    </tr>
</tbody></table>
";

        return systemPrompt;
    }

    private async Task<IReadOnlyList<string>> FindExpertiseMatchesAsync(string input)
    {
        var matches = await jobsService.FindExpertiseMatchesAsync(input, 10);
        return matches;
    }

    private async Task<IEnumerable<Job>> SearchJobsAsync(List<string> expertises, decimal latitude, decimal longitude, int radiusMiles)
    {
        return await jobsService.SearchJobsAsync(expertises, latitude, longitude, radiusMiles);
    }

    /// <summary>
    /// This is a ChatTool that wires up the data retrieval function to be used in a chat.
    /// Description only since the function does not take any parameters since the target GetCurrentLocation in theory uses the device's loaction
    /// </summary>
    /// arrays - https://community.openai.com/t/function-call-is-invalid-please-help/266803/6
    //private readonly ChatTool getValidExpertises = ChatTool.CreateFunctionTool(
    //    functionName: nameof(GetValidExpertises),
    //    functionDescription: "Get the list of valid expertises."
    //);
    private readonly ChatTool findExpertiseMatches = ChatTool.CreateFunctionTool(
        functionName: nameof(FindExpertiseMatchesAsync),
        functionDescription: "Find closest matching allowed expertises.",
        functionParameters: BinaryData.FromBytes("""
        {
            "type": "object",
            "additionalProperties": false,
            "properties": {
                "input": {
                    "type": "string",
                    "description": "The expertise entered by the user."
                }
            },
            "required":["input"]
        }
        """u8.ToArray()),
        functionSchemaIsStrict: true
    );

    private readonly ChatTool searchJobs = ChatTool.CreateFunctionTool(
        functionName: nameof(SearchJobsAsync),
        functionDescription: "Find jobs based on the allowed expertise list and location (using latitude, longitude, and radius).",
        functionParameters: BinaryData.FromBytes("""
        {
            "type": "object",
            "additionalProperties": false,
            "properties": {
                "expertises": {
                    "type": "array",
                    "description": "The list of allowed expertises.",
                    "items": {
                      "type": "string"
                    }
                },
                "latitude": {
                    "type": "number",
                    "description": "The latitude of the location entered by the user."
                },
                "longitude": {
                    "type": "number",
                    "description": "The longitude of the location entered by the user."
                },
                "radius": {
                    "type": "number",
                    "description": "The radius in miles from the location entered by the user."
                }
            },
            "required":["expertises", "latitude", "longitude", "radius"]
        }
        """u8.ToArray()),
        functionSchemaIsStrict: true
    );


    //public async Task ChatCompletionWithToolsAsync(List<ChatMessage> messages)
    //{
    //    //options for the chat - identify the tools available to the model
    //    ChatCompletionOptions options = new()
    //    {
    //        Tools = { findExpertiseMatches, searchJobs }
    //    };

    //    await chatService.ChatCompletionWithTools(messages, options, ToolsCallback);
    //}

    private async Task ToolsCallback(List<ChatMessage> messages, IReadOnlyList<ChatToolCall> toolCalls)
    {
        // Then, add a new tool message for each tool call that is resolved.
        // Should be processed in parallel if possible.
        foreach (ChatToolCall toolCall in toolCalls)
        {
            switch (toolCall.FunctionName)
            {
                case nameof(FindExpertiseMatchesAsync):
                    {
                        using JsonDocument argumentsJson = JsonDocument.Parse(toolCall.FunctionArguments);
                        bool hasParamInput = argumentsJson.RootElement.TryGetProperty("input", out JsonElement elInput);
                        var toolResult = await FindExpertiseMatchesAsync(elInput.GetString()!);
                        var toolResultMessage = string.Join(", ", toolResult);
                        messages.Add(new ToolChatMessage(toolCall.Id, toolResultMessage));
                        break;
                    }

                case nameof(SearchJobsAsync):
                    {
                        using JsonDocument argumentsJson = JsonDocument.Parse(toolCall.FunctionArguments);
                        bool hasParamExpertises = argumentsJson.RootElement.TryGetProperty("expertises", out JsonElement elExpertises);
                        var paramExpertises = elExpertises.EnumerateArray().Select(e => e.GetString()!).ToList();
                        bool hasParamLatitude = argumentsJson.RootElement.TryGetProperty("latitude", out JsonElement elLatitude);
                        var paramLatitude = elLatitude.GetDecimal()!;
                        bool hasParamLongitude = argumentsJson.RootElement.TryGetProperty("longitude", out JsonElement elLongitude);
                        var paramLongitude = elLongitude.GetDecimal()!;
                        bool hasParamRadius = argumentsJson.RootElement.TryGetProperty("radius", out JsonElement elRadius);
                        var paramRadius = elRadius.GetInt32()!;
                        var toolResult = await SearchJobsAsync(paramExpertises, paramLatitude, paramLongitude, paramRadius);
                        var toolResultMessage = string.Join(", ", toolResult);
                        messages.Add(new ToolChatMessage(toolCall.Id, toolResultMessage));
                        break;
                    }

                default:
                    {
                        // Handle other unexpected calls.
                        throw new NotImplementedException();
                    }
            }
        }
    }

}
