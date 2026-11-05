using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StroyControl.Core.Entities;

namespace StroyControl.Data.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Login).HasMaxLength(64).IsRequired();
        builder.HasIndex(u => u.Login).IsUnique();

        builder.Property(u => u.DisplayName).HasMaxLength(150).IsRequired();
        builder.Property(u => u.PasswordHash).HasMaxLength(256).IsRequired();
        builder.Property(u => u.PasswordSalt).HasMaxLength(256).IsRequired();
        builder.Property(u => u.Role).HasConversion<int>();
        builder.Property(u => u.Email).HasMaxLength(150);
        builder.Property(u => u.Phone).HasMaxLength(32);
        builder.Property(u => u.RowVersion).IsConcurrencyToken();
    }
}
