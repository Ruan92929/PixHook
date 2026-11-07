using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PixHook.Domain.Entities;

namespace PixHook.Infrastructure.Persistence.Configurations;

internal sealed class WebhookEventTemplateConfiguration : IEntityTypeConfiguration<WebhookEventTemplate>
{
    public void Configure(EntityTypeBuilder<WebhookEventTemplate> b)
    {
        b.ToTable("EventTemplates");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.EventType).HasConversion<string>().HasMaxLength(50).IsRequired();
        b.Property(x => x.Name).HasMaxLength(150).IsRequired();
        b.Property(x => x.Description).HasMaxLength(500).IsRequired();
        b.Property(x => x.PayloadTemplateJson).IsRequired();
        b.Property(x => x.HttpHeadersTemplateJson).IsRequired();
        b.Property(x => x.SignatureStrategyKey).HasMaxLength(50);
        b.HasIndex(x => x.ProviderId).HasDatabaseName("IX_EventTemplates_ProviderId");
    }
}
