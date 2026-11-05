using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StroyControl.Core.Entities;

namespace StroyControl.Data.Configurations;

public sealed class DeliveryConfiguration : IEntityTypeConfiguration<Delivery>
{
    public void Configure(EntityTypeBuilder<Delivery> builder)
    {
        builder.ToTable("deliveries");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Number).HasMaxLength(32).IsRequired();
        builder.HasIndex(d => d.Number).IsUnique();
        builder.Property(d => d.InvoiceNumber).HasMaxLength(64);
        builder.Property(d => d.Note).HasMaxLength(500);
        builder.Property(d => d.RowVersion).IsConcurrencyToken();

        builder.HasOne(d => d.Supplier)
            .WithMany()
            .HasForeignKey(d => d.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.Project)
            .WithMany()
            .HasForeignKey(d => d.ProjectId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
