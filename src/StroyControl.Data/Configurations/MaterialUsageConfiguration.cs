using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StroyControl.Core.Entities;

namespace StroyControl.Data.Configurations;

public sealed class MaterialUsageConfiguration : IEntityTypeConfiguration<MaterialUsage>
{
    public void Configure(EntityTypeBuilder<MaterialUsage> builder)
    {
        builder.ToTable("material_usages");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Quantity).HasPrecision(18, 3);
        builder.Property(u => u.UnitPrice).HasPrecision(18, 2);
        builder.Property(u => u.DocumentNumber).HasMaxLength(32);
        builder.Property(u => u.Note).HasMaxLength(500);
        builder.Property(u => u.RowVersion).IsConcurrencyToken();

        builder.HasIndex(u => new { u.ProjectId, u.MaterialId });

        builder.HasOne(u => u.Project)
            .WithMany(p => p.Usages)
            .HasForeignKey(u => u.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(u => u.Material)
            .WithMany()
            .HasForeignKey(u => u.MaterialId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(u => u.Estimate)
            .WithMany()
            .HasForeignKey(u => u.EstimateId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(u => u.CreatedBy)
            .WithMany()
            .HasForeignKey(u => u.CreatedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
