using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StroyControl.Core.Entities;

namespace StroyControl.Data.Configurations;

public sealed class MaterialConfiguration : IEntityTypeConfiguration<Material>
{
    public void Configure(EntityTypeBuilder<Material> builder)
    {
        builder.ToTable("materials");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Code).HasMaxLength(32).IsRequired();
        builder.HasIndex(m => m.Code).IsUnique();
        builder.Property(m => m.Name).HasMaxLength(200).IsRequired();
        builder.Property(m => m.Category).HasMaxLength(80);
        builder.Property(m => m.Unit).HasConversion<int>();
        builder.Property(m => m.Price).HasPrecision(18, 2);
        builder.Property(m => m.StockQuantity).HasPrecision(18, 3);
        builder.Property(m => m.MinStock).HasPrecision(18, 3);
        builder.Property(m => m.Note).HasMaxLength(500);
        builder.Property(m => m.RowVersion).IsConcurrencyToken();

        builder.HasOne(m => m.Supplier)
            .WithMany(s => s.Materials)
            .HasForeignKey(m => m.SupplierId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
