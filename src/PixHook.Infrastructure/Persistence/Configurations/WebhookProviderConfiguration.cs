using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PixHook.Domain.Entities;

namespace PixHook.Infrastructure.Persistence.Configurations;

internal sealed class WebhookProviderConfiguration : IEntityTypeConfiguration<WebhookProvider>
{
    public void Configure(EntityTypeBuilder<WebhookProvider> b)
    {
        b.ToTable("Providers");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.Property(x => x.Slug).HasMaxLength(100).IsRequired();
        b.Property(x => x.Description).HasMaxLength(500).IsRequired();
        b.Property(x => x.IsActive).IsRequired();
        b.HasIndex(x => x.Slug).IsUnique().HasDatabaseName("IX_Providers_Slug");

        b.HasMany(x => x.Templates)
            .WithOne(t => t.Provider)
            .HasForeignKey(t => t.ProviderId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Navigation(x => x.Templates).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
