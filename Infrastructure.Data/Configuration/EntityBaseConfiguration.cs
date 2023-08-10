﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Package.Infrastructure.Data.Contracts;

namespace Infrastructure.Data.Configuration;

public abstract class EntityBaseConfiguration<T> : IEntityTypeConfiguration<T> where T : EntityBase
{
    public virtual void Configure(EntityTypeBuilder<T> builder)
    {
        //Guid PK
        builder.HasKey(e => e.Id).IsClustered(false);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.CreatedBy).HasMaxLength(100);
        builder.Property(e => e.UpdatedBy).HasMaxLength(100);
        builder.Property(e => e.CreatedDate).HasColumnType("datetime2(0)");
        builder.Property(e => e.UpdatedDate).HasColumnType("datetime2(0)");
        builder.Property(e => e.RowVersion).IsRowVersion();

        //property converters if needed
        //builder.Property(p => p.Tags).HasConversion(
        //        v => JsonSerializer.Serialize(v),
        //        v => JsonSerializer.Deserialize<List<string>>(v));

        //https://dotnetcoretutorials.com/rowversion-vs-concurrencytoken-in-entityframework-efcore/?expand_article=1
        //shadow property for RowVersion - causes concurrency problems when not tracked
        //builder.Property<byte[]>("RowVersion").IsRowVersion(); //.IsConcurrencyToken(true) when we manually change the value

    }
}
