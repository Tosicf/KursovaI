using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StroyControl.Core.Entities;

namespace StroyControl.Data.Configurations;

public sealed class EstimateConfiguration : IEntityTypeConfiguration<Estimate>
{
    public void Configure(EntityTypeBuilder<Estimate> builder)
    {
        builder.ToTable("estimates");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Number).HasMaxLength(32).IsRequired();
        builder.HasIndex(e => e.Number).IsUnique();
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Status).HasConversion<int>();
        builder.Property(e => e.MarkupPercent).HasPrecision(6, 2).HasDefaultValue(10m);
        builder.Property(e => e.ProfitPercent).HasPrecision(6, 2).HasDefaultValue(12m);
        builder.Property(e => e.LaborSharePercent).HasPrecision(6, 2).HasDefaultValue(35m);
        builder.Property(e => e.Note).HasMaxLength(500);
        builder.Property(e => e.RowVersion).IsConcurrencyToken();

        builder.HasOne(e => e.Project)
            .WithMany(p => p.Estimates)
            .HasForeignKey(e => e.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
