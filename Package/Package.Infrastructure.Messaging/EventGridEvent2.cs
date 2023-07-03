﻿namespace Package.Infrastructure.Messaging;

//Map to EventGridEvent; so client modules don't need a reference to Azure SDK
public class EventGridEvent2
{
    public EventGridEvent2(string subject, string eventType, string dataVersion, object data, Type? dataSerializationType = null)
    {
        Subject = subject;
        EventType = eventType;
        DataVersion = dataVersion;
        Data = data;
        DataSerializationType = dataSerializationType;
    }

    public Type? DataSerializationType { get; set; }

    //
    // Summary:
    //     Gets or sets a unique identifier for the event.
    public string? Id { get; set; }
    //
    // Summary:
    //     Gets or sets the event payload as System.BinaryData. Using BinaryData, one can
    //     deserialize the payload into rich data, or access the raw JSON data using System.BinaryData.ToString
    public object? Data { get; set; }
    //
    // Summary:
    //     Gets or sets the resource path of the event source. This must be set when publishing
    //     the event to a domain, and must not be set when publishing the event to a topic.
    public string? Topic { get; set; }
    //
    // Summary:
    //     Gets or sets a resource path relative to the topic path.
    public string Subject { get; set; }
    //
    // Summary:
    //     Gets or sets the type of the event that occurred.
    public string EventType { get; set; }
    //
    // Summary:
    //     Gets or sets the time (in UTC) the event was generated.
    public DateTimeOffset? EventTime { get; set; }
    //
    // Summary:
    //     Gets or sets the schema version of the data object.
    public string DataVersion { get; set; }
}
