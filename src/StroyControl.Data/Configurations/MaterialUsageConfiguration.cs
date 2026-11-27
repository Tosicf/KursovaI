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

        // SQL Server запрещает две каскадные цепочки к одной таблице: списание уже
        // связано со сметой (SET NULL), а смета каскадно связана с позициями и актами,
        // поэтому ссылки на позицию сметы и на акт работ создаются без действий в базе
        // (NO ACTION). Нужные записи сервисы удаляют явно, в одной транзакции.
        builder.HasOne(u => u.EstimateItem)
            .WithMany()
            .HasForeignKey(u => u.EstimateItemId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        builder.HasOne(u => u.WorkCompletion)
            .WithMany(c => c.MaterialUsages)
            .HasForeignKey(u => u.WorkCompletionId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        builder.HasOne(u => u.CreatedBy)
            .WithMany()
            .HasForeignKey(u => u.CreatedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
