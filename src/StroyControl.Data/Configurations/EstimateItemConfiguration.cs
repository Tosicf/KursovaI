using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StroyControl.Core.Entities;

namespace StroyControl.Data.Configurations;

public sealed class EstimateItemConfiguration : IEntityTypeConfiguration<EstimateItem>
{
    public void Configure(EntityTypeBuilder<EstimateItem> builder)
    {
        builder.ToTable("estimate_items");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Section).HasMaxLength(120);
        builder.Property(i => i.PlannedQuantity).HasPrecision(18, 3);
        builder.Property(i => i.PlannedUnitPrice).HasPrecision(18, 2);
        builder.Property(i => i.IsWork).HasDefaultValue(false);
        builder.Property(i => i.WorkName).HasMaxLength(200);
        builder.Property(i => i.WorkUnit).HasMaxLength(16);
        builder.Property(i => i.Note).HasMaxLength(300);
        builder.Property(i => i.RowVersion).IsConcurrencyToken();

        builder.HasOne(i => i.Estimate)
            .WithMany(e => e.Items)
            .HasForeignKey(i => i.EstimateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.Material)
            .WithMany()
            .HasForeignKey(i => i.MaterialId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
