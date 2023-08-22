﻿using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace SampleApp.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class EventGridController : ControllerBase
{
    private readonly ILogger<EventGridController> _logger;

    //initial one-time EventGrid validation of the target
    private bool EventTypeSubscriptionValidation =>
        HttpContext.Request.Headers["aeg-event-type"].FirstOrDefault() == "SubscriptionValidation";

    //subsequent EventGrid event notifications
    private bool EventTypeSubscriptionNotification =>
        HttpContext.Request.Headers["aeg-event-type"].FirstOrDefault() == "Notification";

    public EventGridController(ILogger<EventGridController> logger)
    {
        _logger = logger;
    }


    //for local testing create a tunnel - VS Dev Tunnels, ngrok
    //https://docs.microsoft.com/en-us/azure/event-grid/secure-webhook-delivery
    [AllowAnonymous]
    [HttpPost("event")]
    public async Task<IActionResult> Event()
    {
        using var requestStream = new StreamReader(Request.Body);
        var bodyJson = await requestStream.ReadToEndAsync();
        var events = JsonSerializer.Deserialize<List<EventGridEvent>>(bodyJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (events == null) return BadRequest($"EventGrid body does not deserialize in to List<EventGridEvent>: {bodyJson}");

        //initial call from EventGrid upon creating a Subscription is a one-time validation of the target - complete the event subscription validation handshake
        if (EventTypeSubscriptionValidation)
        {
            //string eventData = Encoding.UTF8.GetString(events.First().Data);
            //var data = JsonSerializer.Deserialize<SubscriptionValidationEventData>(eventData);
            //string? vcode = data!.ValidationCode;
            return new OkObjectResult(new SubscriptionValidationResponse());
        }
        //subsequent calls from EventGrid for a subscription contain normal events for processing
        else if (EventTypeSubscriptionNotification)
        {
            //process the events concurrently - should be fast to return an OK to event grid
            var tasks = events.Select(e => ProcessEventGridEvent(e));
            Task.WaitAll(tasks.ToArray());

            return Ok();
        }

        return new BadRequestResult();
    }

    private async Task ProcessEventGridEvent(EventGridEvent egEvent)
    {
        var eData = egEvent.Data?.ToString(); //extract from inputEvent Encoding.UTF8.GetString(egEvent.Data);
        _logger.LogInformation("ProcessEventGridEvent {EventData}", eData);
        //await some service call
        await Task.CompletedTask;
    }
}
