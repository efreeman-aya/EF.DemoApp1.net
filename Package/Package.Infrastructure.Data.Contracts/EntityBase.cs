﻿namespace Package.Infrastructure.Data.Contracts;

public class EntityBase : IAuditable
{
    private readonly Guid _id = Guid.NewGuid();
    public EntityBase()
    {
    }

    public Guid Id
    {
        get { return _id; }
        init { if (value != Guid.Empty) _id = value; }
    }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = "New";
    public DateTime UpdatedDate { get; set; }
    public string? UpdatedBy { get; set; }
}
