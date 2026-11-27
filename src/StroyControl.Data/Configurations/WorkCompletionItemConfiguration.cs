using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StroyControl.Core.Entities;

namespace StroyControl.Data.Configurations;

public sealed class WorkCompletionItemConfiguration : IEntityTypeConfiguration<WorkCompletionItem>
{
    public void Configure(EntityTypeBuilder<WorkCompletionItem> builder)
    {
        builder.ToTable("work_completion_items");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Quantity).HasPrecision(18, 3);
        builder.Property(i => i.UnitPrice).HasPrecision(18, 2);
        builder.Property(i => i.RowVersion).IsConcurrencyToken();

        builder.HasOne(i => i.WorkCompletion)
            .WithMany(c => c.Items)
            .HasForeignKey(i => i.WorkCompletionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.EstimateItem)
            .WithMany()
            .HasForeignKey(i => i.EstimateItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
