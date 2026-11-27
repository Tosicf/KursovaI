using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StroyControl.Core.Entities;

namespace StroyControl.Data.Configurations;

public sealed class WorkCompletionConfiguration : IEntityTypeConfiguration<WorkCompletion>
{
    public void Configure(EntityTypeBuilder<WorkCompletion> builder)
    {
        builder.ToTable("work_completions");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.DocumentNumber).HasMaxLength(32).IsRequired();
        builder.HasIndex(c => c.DocumentNumber).IsUnique();
        builder.HasIndex(c => c.Date);
        builder.Property(c => c.Note).HasMaxLength(500);
        builder.Property(c => c.RowVersion).IsConcurrencyToken();

        // Каскадное удаление акта вместе со сметой дало бы вторую цепочку к списаниям,
        // поэтому связь ограничивающая: смету с документами удалить нельзя, а акт
        // удаляется вместе со строками (см. WorkCompletionItemConfiguration).
        builder.HasOne(c => c.Estimate)
            .WithMany()
            .HasForeignKey(c => c.EstimateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Project)
            .WithMany()
            .HasForeignKey(c => c.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.CreatedBy)
            .WithMany()
            .HasForeignKey(c => c.CreatedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
