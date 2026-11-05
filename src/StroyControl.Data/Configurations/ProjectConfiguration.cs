using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StroyControl.Core.Entities;

namespace StroyControl.Data.Configurations;

public sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("projects");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Code).HasMaxLength(32).IsRequired();
        builder.HasIndex(p => p.Code).IsUnique();
        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Address).HasMaxLength(250);
        builder.Property(p => p.Customer).HasMaxLength(200);
        builder.Property(p => p.ManagerName).HasMaxLength(150);
        builder.Property(p => p.ContractSum).HasPrecision(18, 2);
        builder.Property(p => p.ProgressPercent);
        builder.Property(p => p.Status).HasConversion<int>();
        builder.Property(p => p.Note).HasMaxLength(500);
        builder.Property(p => p.RowVersion).IsConcurrencyToken();
    }
}
